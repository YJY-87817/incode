// 網站色票（https://colorhunt.co/palette/131b2b2f4a635b91a3cfeae0），給 Vuetify 主題使用。
// CSS 請用 src/assets/css/theme.css 的變數（var(--incode-primary) 等）；
// Material Kit（Bootstrap）的 primary 在 src/assets/scss/material-kit/custom/_variables.scss。改色時三處一起改。
export const themeColors = {
  "brand-light": "#CFEAE0", // 淺色底
  accent: "#5B91A3", // 點綴、標題列底色
  primary: "#2F4A63", // 主色：導覽列、按鈕、連結
  "brand-dark": "#131B2B", // 文字、主要按鈕、深色區塊
};

export const vuetifyTheme = {
  defaultTheme: "light",
  themes: {
    light: {
      colors: {
        ...themeColors,
        secondary: themeColors.accent,
      },
    },
  },
};
