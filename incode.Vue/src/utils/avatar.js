export const getAvatarUrl = (name, explicitUrl = "") => {
  if (explicitUrl) return explicitUrl;
  const safeName = String(name || "User").trim() || "User";
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(safeName)}&background=random`;
};
