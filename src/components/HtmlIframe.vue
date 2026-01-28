<template>
  <iframe
    ref="frame"
    class="html-iframe"
    :class="{ 'html-iframe--inline': inline }"
    :style="iframeStyle"
    :srcdoc="srcdoc"
    :sandbox="sandbox"
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
  sandbox: {
    type: String,
    default: "allow-same-origin",
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
    img { max-width: 100%; height: auto; display: block; margin: 0.5rem 0; }
    p { margin: 0 0 0.5rem 0; }
    a { color: inherit; }
    ${props.extraCss || ""}
  `;
});

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
  <body>${props.html || ""}</body>
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
  }
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
