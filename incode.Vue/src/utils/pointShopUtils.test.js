import { describe, it, expect } from "vitest";
import {
  escapeHtml, normalizePhone, isValidMobile, validateShipping,
  ensureOk, ApiFailError, isBusinessError, errorText,
} from "./pointShopUtils.js";

describe("pointShopUtils", () => {
  it("escapeHtml 跳脫特殊字元", () => {
    expect(escapeHtml(`<a href="x">'&'</a>`)).toBe("&lt;a href=&quot;x&quot;&gt;&#39;&amp;&#39;&lt;/a&gt;");
  });

  it("手機號碼檢查", () => {
    expect(normalizePhone(" 0912-345 678 ")).toBe("0912345678");
    expect(isValidMobile("0912-345-678")).toBe(true);
    expect(isValidMobile("0812345678")).toBe(false);
    expect(isValidMobile("091234567")).toBe(false);
  });

  it("validateShipping", () => {
    const ok = { name: "王小明", phone: "0912345678", address: "台北市信義區" };
    expect(validateShipping(ok)).toBeNull();
    expect(validateShipping({ ...ok, name: "" })).toMatch(/姓名/);
    expect(validateShipping({ ...ok, phone: "123" })).toMatch(/手機/);
    expect(validateShipping({ ...ok, address: "短" })).toMatch(/地址/);
    expect(validateShipping({ ...ok, name: "A" })).toMatch(/2-10/);
  });

  it("ensureOk / isBusinessError / errorText", () => {
    expect(() => ensureOk({ data: { ok: false, message: "庫存不足" } })).toThrow("庫存不足");
    const data = { ok: true };
    expect(ensureOk({ data })).toBe(data);

    expect(isBusinessError(new ApiFailError("x"))).toBe(true);
    expect(isBusinessError({ response: { status: 409 } })).toBe(true);
    expect(isBusinessError({ response: { status: 401 } })).toBe(false);
    expect(isBusinessError({ response: { status: 500 } })).toBe(false);
    expect(isBusinessError(new Error("network"))).toBe(false);

    expect(errorText({ response: { data: { message: "點數不足" } } })).toBe("點數不足");
    expect(errorText(new Error("x"), "fallback")).toBe("fallback");
  });
});
