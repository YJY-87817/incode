import { beforeEach, describe, expect, it } from "vitest";
import { createPinia, setActivePinia } from "pinia";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";

describe("forumStore 結構", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("CONTENT_TYPES 維持不變", () => {
    expect(CONTENT_TYPES).toEqual({ POST: 1, REPLY: 2, COMMENT: 3, COMMENT_REPLY: 4 });
  });

  it("state / getters / actions 的名稱與型別", () => {
    const store = useForumStore();
    const shape = Object.keys(store)
      .filter((key) => !key.startsWith("$") && !key.startsWith("_"))
      .sort()
      .map((key) => `${key}:${typeof store[key]}`);
    expect(shape).toMatchSnapshot();
  });

  it("初始 state", () => {
    const store = useForumStore();
    expect(JSON.parse(JSON.stringify(store.$state))).toMatchSnapshot();
  });
});
