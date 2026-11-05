import { h } from "vue";
import { aliases, mdi as mdiSvg } from "vuetify/iconsets/mdi-svg";
import {
  mdiAccountCircleOutline,
  mdiAccountOutline,
  mdiArrowLeft,
  mdiArrowRight,
  mdiBookmark,
  mdiBookmarkOutline,
  mdiCakeVariantOutline,
  mdiCalendarBlank,
  mdiCamera,
  mdiCameraPlus,
  mdiCartOutline,
  mdiCashPlus,
  mdiCheck,
  mdiCheckCircle,
  mdiChevronDown,
  mdiChevronUp,
  mdiClipboardTextSearchOutline,
  mdiClockOutline,
  mdiClose,
  mdiCloudUpload,
  mdiDeleteOutline,
  mdiEmailOutline,
  mdiEye,
  mdiEyeOff,
  mdiFaceManProfile,
  mdiFlagVariantOutline,
  mdiGoogle,
  mdiImage,
  mdiLockCheckOutline,
  mdiLockOutline,
  mdiLogout,
  mdiMagnify,
  mdiMapMarkerOutline,
  mdiPencilOutline,
  mdiPhoneOutline,
  mdiPlus,
  mdiShareVariantOutline,
  mdiStarOutline,
} from "@mdi/js";

/**
 * Vuetify 圖示：以 SVG 顯示 Material Design Icons，只打包下面列出的圖示。
 * （原本載入整個 @mdi/font：CSS 418 KB + 字型 403 KB，實際只用到約 40 個圖示）
 *
 * 模板維持原寫法：<v-icon>mdi-eye</v-icon>、icon="mdi-eye"、prepend-inner-icon="mdi-eye"。
 * **用到新的圖示時要在這裡加上**（從 @mdi/js 匯入，名稱 mdi-xxx-yyy → mdiXxxYyy），沒登記的圖示會顯示空白並在 console 警告。
 * 只能用在 Vuetify 元件；不要再寫 <i class="mdi mdi-xxx">（已沒有字型 CSS）。
 */
const icons = {
  "mdi-account-circle-outline": mdiAccountCircleOutline,
  "mdi-account-outline": mdiAccountOutline,
  "mdi-arrow-left": mdiArrowLeft,
  "mdi-arrow-right": mdiArrowRight,
  "mdi-bookmark": mdiBookmark,
  "mdi-bookmark-outline": mdiBookmarkOutline,
  "mdi-cake-variant-outline": mdiCakeVariantOutline,
  "mdi-calendar-blank": mdiCalendarBlank,
  "mdi-camera": mdiCamera,
  "mdi-camera-plus": mdiCameraPlus,
  "mdi-cart-outline": mdiCartOutline,
  "mdi-cash-plus": mdiCashPlus,
  "mdi-check": mdiCheck,
  "mdi-check-circle": mdiCheckCircle,
  "mdi-chevron-down": mdiChevronDown,
  "mdi-chevron-up": mdiChevronUp,
  "mdi-clipboard-text-search-outline": mdiClipboardTextSearchOutline,
  "mdi-clock-outline": mdiClockOutline,
  "mdi-close": mdiClose,
  "mdi-cloud-upload": mdiCloudUpload,
  "mdi-delete-outline": mdiDeleteOutline,
  "mdi-email-outline": mdiEmailOutline,
  "mdi-eye": mdiEye,
  "mdi-eye-off": mdiEyeOff,
  "mdi-face-man-profile": mdiFaceManProfile,
  "mdi-flag-variant-outline": mdiFlagVariantOutline,
  "mdi-google": mdiGoogle,
  "mdi-image": mdiImage,
  "mdi-lock-check-outline": mdiLockCheckOutline,
  "mdi-lock-outline": mdiLockOutline,
  "mdi-logout": mdiLogout,
  "mdi-magnify": mdiMagnify,
  "mdi-map-marker-outline": mdiMapMarkerOutline,
  "mdi-pencil-outline": mdiPencilOutline,
  "mdi-phone-outline": mdiPhoneOutline,
  "mdi-plus": mdiPlus,
  "mdi-share-variant-outline": mdiShareVariantOutline,
  "mdi-star-outline": mdiStarOutline,
};

const warned = new Set();

const mdi = {
  component: (props) => {
    const path = icons[props.icon];
    if (!path && !warned.has(props.icon)) {
      warned.add(props.icon);
      console.warn(`[vuetifyIcons] 未登記的圖示：${props.icon}，請加到 src/plugins/vuetifyIcons.js`);
    }
    return h(mdiSvg.component, { ...props, icon: path ?? "" });
  },
};

export const iconsOptions = {
  defaultSet: "mdi",
  aliases, // Vuetify 元件內建圖示（關閉、勾選框、下拉…）使用 SVG 路徑
  sets: { mdi },
};
