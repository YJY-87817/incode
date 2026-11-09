import { describe, it, expect, vi, beforeEach } from "vitest";

const authStore = { isAuthenticated: true, user: { id: 1 } };
const router = { currentRoute: { value: { name: "Home", fullPath: "/p" } }, push: vi.fn() };

vi.mock("sweetalert2", () => ({ default: { fire: vi.fn() } }));
vi.mock("@/stores/users/auth.js", () => ({ useAuthStore: () => authStore }));
vi.mock("@/router", () => ({ default: router }));

import { installAuthInterceptor } from "./authInterceptor.js";

function setup(opts) {
  let onError;
  installAuthInterceptor({ interceptors: { response: { use: (_ok, err) => (onError = err) } } }, opts);
  return onError;
}

beforeEach(() => {
  vi.useFakeTimers();
  vi.advanceTimersByTime(3000); // 重置模組內的 2 秒去重旗標
  authStore.isAuthenticated = true;
  authStore.user = { id: 1 };
  router.push.mockClear();
  router.currentRoute.value = { name: "Home", fullPath: "/p" };
});

describe("installAuthInterceptor", () => {
  it("已登入收到 401：清除狀態、導向登入頁並帶 redirect，錯誤繼續拋出", async () => {
    const onError = setup();
    const err = { response: { status: 401 } };
    await expect(onError(err)).rejects.toBe(err);
    expect(authStore.isAuthenticated).toBe(false);
    expect(authStore.user).toBeNull();
    expect(router.push).toHaveBeenCalledWith({ name: "Login", query: { redirect: "/p" } });
  });

  it("未登入收到 401：預設不處理", async () => {
    authStore.isAuthenticated = false;
    const onError = setup();
    await expect(onError({ response: { status: 401 } })).rejects.toBeDefined();
    expect(router.push).not.toHaveBeenCalled();
  });

  it("未登入且 redirectWhenAnonymous：導向登入頁", async () => {
    authStore.isAuthenticated = false;
    const onError = setup({ redirectWhenAnonymous: true });
    await expect(onError({ response: { status: 401 } })).rejects.toBeDefined();
    expect(router.push).toHaveBeenCalledTimes(1);
  });

  it("非 401 錯誤不處理", async () => {
    const onError = setup();
    await expect(onError({ response: { status: 500 } })).rejects.toBeDefined();
    expect(router.push).not.toHaveBeenCalled();
    expect(authStore.isAuthenticated).toBe(true);
  });

  it("已在登入頁不再導向", async () => {
    router.currentRoute.value = { name: "Auth", fullPath: "/auth" };
    const onError = setup();
    await expect(onError({ response: { status: 401 } })).rejects.toBeDefined();
    expect(router.push).not.toHaveBeenCalled();
  });

  it("2 秒內多個 401 只導向一次", async () => {
    const onError = setup();
    await Promise.allSettled([onError({ response: { status: 401 } }), onError({ response: { status: 401 } })]);
    expect(router.push).toHaveBeenCalledTimes(1);
  });
});
