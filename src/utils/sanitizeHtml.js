import DOMPurify from "dompurify";

/**
 * Sanitize rich HTML content and normalize asset URLs for safe rendering.
 * @param {string} html - Raw HTML string from the server.
 * @param {(url: string) => string} [normalizeUrl] - Optional normalizer for src/href values.
 * @returns {{ sanitizedHtml: string, textContent: string, imageUrls: string[] }}
 */
export function normalizeRichContent(html, normalizeUrl) {
  const clean = DOMPurify.sanitize(html || "", {
    USE_PROFILES: { html: true },
    ADD_ATTR: ["target", "rel"],
  });

  if (!clean) {
    return { sanitizedHtml: "", textContent: "", imageUrls: [] };
  }

  const parser = new DOMParser();
  const doc = parser.parseFromString(clean, "text/html");
  const images = [];

  doc.querySelectorAll("img").forEach((img) => {
    const original = img.getAttribute("src") || "";
    const normalized = typeof normalizeUrl === "function"
      ? normalizeUrl(original)
      : original;

    if (normalized) {
      img.setAttribute("src", normalized);
      images.push(normalized);
    } else {
      img.removeAttribute("src");
    }
  });

  doc.querySelectorAll("a").forEach((anchor) => {
    const rel = anchor.getAttribute("rel") || "";
    const relTokens = new Set(rel.split(/\s+/).filter(Boolean));
    relTokens.add("noopener");
    relTokens.add("noreferrer");
    anchor.setAttribute("rel", Array.from(relTokens).join(" "));
    if (!anchor.getAttribute("target")) {
      anchor.setAttribute("target", "_blank");
    }
  });

  return {
    sanitizedHtml: doc.body.innerHTML,
    textContent: doc.body.textContent?.trim() || "",
    imageUrls: Array.from(new Set(images)),
  };
}
