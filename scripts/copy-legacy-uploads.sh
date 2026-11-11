#!/usr/bin/env bash
# 把改版前提交在 incode.MVC/wwwroot 的上傳檔複製到 App:UploadRoot（網址不變：/img/chat/xxx 會改由 UploadRoot 提供）。
#
# 這些檔案已從 git 移除（之後 pull 會從 wwwroot 刪掉），每個環境（開發機、正式機）都要先執行一次。
# 已存在的檔案不會覆蓋，可以重複執行。Windows 請用 Git Bash 執行。
#
# 用法（在 repo 根目錄執行）：
#   scripts/copy-legacy-uploads.sh [UploadRoot] [來源]
#
#   UploadRoot  預設 incode.MVC/App_Data/uploads（與 appsettings.json 的 App:UploadRoot 預設值相同）；
#               正式環境請填 App__UploadRoot 設定的絕對路徑
#   來源        預設 git:a1199c9（移除前最後一個包含這些檔案的 commit，不需要網路，只要本機有完整歷史）；
#               也可以填備份資料夾的路徑（裡面要有 img/fromApi、img/chat、img/user、img/product、contents）
#
# 例：
#   scripts/copy-legacy-uploads.sh
#   scripts/copy-legacy-uploads.sh /var/incode/uploads
#   scripts/copy-legacy-uploads.sh D:/incode-uploads /c/backup/wwwroot
set -euo pipefail

UPLOAD_ROOT="${1:-incode.MVC/App_Data/uploads}"
SOURCE="${2:-git:a1199c9}"
FOLDERS=(img/fromApi img/chat img/user img/product contents)

tmp=""
cleanup() { [[ -n "$tmp" ]] && rm -rf "$tmp"; }
trap cleanup EXIT

if [[ "$SOURCE" == git:* ]]; then
  rev="${SOURCE#git:}"
  git rev-parse --verify --quiet "${rev}^{commit}" > /dev/null || {
    echo "找不到 commit ${rev}：請先 git fetch，或改用備份資料夾當來源" >&2
    exit 1
  }
  tmp="$(mktemp -d)"
  paths=()
  for f in "${FOLDERS[@]}"; do paths+=("incode.MVC/wwwroot/$f"); done
  git archive "$rev" -- "${paths[@]}" | tar -x -C "$tmp"
  src="$tmp/incode.MVC/wwwroot"
else
  src="$SOURCE"
fi

[[ -d "$src" ]] || { echo "來源資料夾不存在：$src" >&2; exit 1; }

echo "來源：$SOURCE"
echo "目的：$UPLOAD_ROOT"
total_new=0
for f in "${FOLDERS[@]}"; do
  if [[ ! -d "$src/$f" ]]; then
    echo "  $f：來源沒有這個資料夾，略過"
    continue
  fi
  mkdir -p "$UPLOAD_ROOT/$f"
  before=$(find "$UPLOAD_ROOT/$f" -type f | wc -l)
  # 已存在的檔案不覆蓋（不用 cp -n：各平台的 cp 行為與支援的選項不同）
  while IFS= read -r -d '' file; do
    rel="${file#"$src/$f/"}"
    dest="$UPLOAD_ROOT/$f/$rel"
    if [[ ! -e "$dest" ]]; then
      mkdir -p "$(dirname "$dest")"
      cp -p "$file" "$dest"
    fi
  done < <(find "$src/$f" -type f -print0)
  after=$(find "$UPLOAD_ROOT/$f" -type f | wc -l)
  echo "  $f：新增 $((after - before)) 個檔案（目前共 $after 個）"
  total_new=$((total_new + after - before))
done
echo "完成，共新增 $total_new 個檔案"
