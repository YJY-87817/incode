<template>
  <iframe
    ref="frame"
    class="html-iframe"
    :class="{ 'html-iframe--inline': inline }"
    :style="iframeStyle"
    :srcdoc="srcdoc"
    :sandbox="effectiveSandbox"
    loading="lazy"
    @load="handleLoad"
  ></iframe>
</template>

<script setup>
import { ref, computed, watch, nextTick, onBeforeUnmount } from "vue";

const props = defineProps({
  html: {
    type: String,
    default: "",
  },
  inline: {
    type: Boolean,
    default: false,
  },
  minHeight: {
    type: Number,
    default: 120,
  },
  minWidth: {
    type: Number,
    default: 0,
  },
  textColor: {
    type: String,
    default: "",
  },
  fontSize: {
    type: String,
    default: "",
  },
  lineHeight: {
    type: String,
    default: "",
  },
  // sandbox 一律保留，且不允許執行腳本（第二層防護：內容已先經 DOMPurify 過濾，萬一漏網也不會執行）。
  // allow-same-origin：讓父頁面能讀取內容高度；allow-popups(-to-escape-sandbox)：內文連結（target=_blank）可以開新分頁。
  // 不要加 allow-scripts：與 allow-same-origin 同時存在時，iframe 內的腳本可以自行移除 sandbox。
  sandbox: {
    type: String,
    default: "allow-same-origin allow-popups allow-popups-to-escape-sandbox",
  },
  includeFontAwesome: {
    type: Boolean,
    default: true,
  },
  includeMaterialIcons: {
    type: Boolean,
    default: true,
  },
  extraCss: {
    type: String,
    default: "",
  },
});

const frame = ref(null);
const height = ref(props.minHeight);
const width = ref(props.minWidth || 0);
let resizeObserver = null;

const baseCss = computed(() => {
  const colorRule = props.textColor ? `color: ${props.textColor};` : "";
  const fontSizeRule = props.fontSize ? `font-size: ${props.fontSize};` : "";
  const lineHeightRule = props.lineHeight
    ? `line-height: ${props.lineHeight};`
    : "";
  return `
    :root { color-scheme: light; }
    *, *::before, *::after {
      scrollbar-width: none;
      -ms-overflow-style: none;
    }
    *::-webkit-scrollbar {
      width: 0;
      height: 0;
      display: none;
    }
    html, body {
      scrollbar-width: none;
      -ms-overflow-style: none;
    }
    html::-webkit-scrollbar, body::-webkit-scrollbar {
      width: 0;
      height: 0;
      display: none;
    }
    body {
      margin: 0;
      padding: 0;
      ${colorRule}
      ${fontSizeRule}
      ${lineHeightRule}
      word-break: break-word;
      font-family: inherit;
      background: transparent;
    }
    img:not([style*="width"]) { width: 100%; }
    img { max-width: 100%; height: auto; display: block; margin: 0.5rem 0; }
    p { margin: 0 0 0.5rem 0; min-height: 1em; }
    p:empty::before { content: '\\200b'; }
    br { display: block; content: ''; margin-top: 0.5em; }
    a { color: inherit; }
    ${props.extraCss || ""}
  `;
});

const stripScripts = (html) => {
  if (!html) return "";

  // 先用正則移除可能的 script 標籤（避免不完整標記被 DOMParser 保留）
  const withoutScripts = html.replace(
    /<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi,
    "",
  );

  const parser = new DOMParser();
  const doc = parser.parseFromString(withoutScripts, "text/html");

  // 再次移除所有 script 標籤
  doc.querySelectorAll("script").forEach((node) => node.remove());

  // 移除所有 on* 事件屬性（onclick, onload, onerror 等）
  doc.querySelectorAll("*").forEach((element) => {
    Array.from(element.attributes).forEach((attr) => {
      const name = attr.name.toLowerCase();
      const value = String(attr.value || "").trim();
      if (name.startsWith("on")) {
        element.removeAttribute(attr.name);
        return;
      }

      // 防止 javascript: URL
      if ((name === "href" || name === "src") && /^javascript:/i.test(value)) {
        element.removeAttribute(attr.name);
      }
    });
  });

  return doc.body.innerHTML;
};

const bodyHtml = computed(() => stripScripts(props.html || ""));

const srcdoc = computed(() => {
  const faLink = props.includeFontAwesome
    ? '<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" integrity="sha512-DTOQO9RWCH3ppGqcWaEA1BIZOC6xxalwEsw9c2QQeAIftl+Vegovlnee1c9QX4TctnWMn13TZye+giMm8e2LwA==" crossorigin="anonymous" referrerpolicy="no-referrer" />'
    : "";
  const materialLink = props.includeMaterialIcons
    ? '<link href="https://fonts.googleapis.com/icon?family=Material+Icons+Round" rel="stylesheet" />'
    : "";
  return `<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    ${faLink}
    ${materialLink}
    <style>${baseCss.value}</style>
  </head>
  <body>${bodyHtml.value}</body>
</html>`;
});

const updateSize = () => {
  const body = frame.value?.contentDocument?.body;
  if (!body) return;
  height.value = Math.max(body.scrollHeight, props.minHeight);
  if (props.inline) {
    width.value = Math.max(body.scrollWidth, props.minWidth || 0);
  }
};

const handleLoad = () => {
  updateSize();
  const body = frame.value?.contentDocument?.body;
  if (!body || typeof ResizeObserver === "undefined") return;
  if (resizeObserver) resizeObserver.disconnect();
  resizeObserver = new ResizeObserver(() => updateSize());
  resizeObserver.observe(body);
};

watch(
  () => props.html,
  async () => {
    await nextTick();
    updateSize();
  },
);

onBeforeUnmount(() => {
  if (resizeObserver) resizeObserver.disconnect();
});

const iframeStyle = computed(() => {
  const style = {
    height: `${height.value}px`,
    width: props.inline ? `${width.value || props.minWidth || 0}px` : "100%",
    display: props.inline ? "inline-block" : "block",
    verticalAlign: props.inline ? "middle" : "initial",
  };
  return style;
});

// 原本傳入 allow-scripts 時會「直接移除 sandbox 屬性」，論壇內容等於沒有 sandbox；現在一律保留且移除 allow-scripts
const effectiveSandbox = computed(() =>
  (props.sandbox || "")
    .split(/\s+/)
    .filter((token) => token && token !== "allow-scripts")
    .join(" "),
);
</script>

<style scoped>
.html-iframe {
  border: 0;
  background: transparent;
  min-height: 1em;
}

.html-iframe--inline {
  min-width: 1em;
}
</style>
