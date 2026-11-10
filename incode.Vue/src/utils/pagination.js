export const buildDisplayPages = (currentPage, totalPages, maxDisplay = 5) => {
  const pages = [];
  if (totalPages <= 1) return pages;

  let start = Math.max(1, currentPage - Math.floor(maxDisplay / 2));
  let end = Math.min(totalPages, start + maxDisplay - 1);

  if (end - start < maxDisplay - 1) {
    start = Math.max(1, end - maxDisplay + 1);
  }

  for (let index = start; index <= end; index += 1) {
    pages.push(index);
  }

  return pages;
};
