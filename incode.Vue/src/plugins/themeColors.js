// 網站色票（https://colorhunt.co/palette/ebebd86375454552309ba685），給 Vuetify 主題使用。
// CSS 請用 src/assets/css/theme.css 的變數（var(--incode-olive) 等）；
// Material Kit（Bootstrap）的 primary 在 src/assets/scss/material-kit/custom/_variables.scss。改色時三處一起改。
export const themeColors = {
  cream: "#EBEBD8", // 背景
  sage: "#9BA685", // 裝飾、淺色底（不要放文字）
  olive: "#637545", // 主色：按鈕、連結
  "olive-dark": "#455230", // 文字、深色區塊
};

export const vuetifyTheme = {
  defaultTheme: "light",
  themes: {
    light: {
      colors: {
        primary: themeColors.olive,
        secondary: themeColors.sage,
        ...themeColors,
      },
    },
  },
};
