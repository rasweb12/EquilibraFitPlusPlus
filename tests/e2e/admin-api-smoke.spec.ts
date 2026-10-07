import { expect, test } from "@playwright/test";

const apiBaseUrl = process.env.E2E_API_BASE_URL;
const aiBaseUrl = process.env.E2E_AI_BASE_URL;
const adminBaseUrl = process.env.E2E_ADMIN_BASE_URL;
const adminEmail = process.env.E2E_ADMIN_EMAIL;
const adminPassword = process.env.E2E_ADMIN_PASSWORD;

test.describe("EquilibraFit++ service smoke", () => {
  test("backend health endpoint responds", async ({ request }) => {
    test.skip(!apiBaseUrl, "Set E2E_API_BASE_URL to run backend smoke.");

    const response = await request.get(`${apiBaseUrl}/health`);

    expect(response.ok()).toBeTruthy();
  });

  test("ai health endpoint responds", async ({ request }) => {
    test.skip(!aiBaseUrl, "Set E2E_AI_BASE_URL to run AI smoke.");

    const response = await request.get(`${aiBaseUrl}/health`);
    const body = await response.json();

    expect(response.ok()).toBeTruthy();
    expect(body.service).toBe("equilibrafit-plusplus-ai");
  });

  test("admin login page loads", async ({ page }) => {
    test.skip(!adminBaseUrl, "Set E2E_ADMIN_BASE_URL to run admin smoke.");

    await page.goto(`${adminBaseUrl}/login`);

    await expect(page.getByText("Acesso restrito para operacao")).toBeVisible();
    await expect(page.getByRole("button", { name: "Entrar" })).toBeVisible();
  });

  test("admin authenticated modules navigate", async ({ page }) => {
    test.skip(
      !adminBaseUrl || !adminEmail || !adminPassword,
      "Set E2E_ADMIN_BASE_URL, E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD to run authenticated admin smoke."
    );

    await page.goto(`${adminBaseUrl}/login`);
    await page.getByLabel("E-mail").fill(adminEmail!);
    await page.getByLabel("Senha").fill(adminPassword!);
    await page.getByRole("button", { name: "Entrar" }).click();

    await expect(page.getByRole("link", { name: "Financeiro" })).toBeVisible();

    for (const moduleName of ["Financeiro", "Marketplace", "Notificacoes", "Relatorios", "Gamificacao", "LGPD"]) {
      await page.getByRole("link", { name: moduleName }).click();
      await expect(page.getByText(moduleName, { exact: true }).first()).toBeVisible();
    }
  });
});
