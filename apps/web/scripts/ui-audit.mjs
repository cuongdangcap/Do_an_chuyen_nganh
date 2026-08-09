import { chromium } from "playwright";
import fs from "node:fs/promises";
import path from "node:path";

const baseURL = process.env.UI_AUDIT_BASE_URL || "http://127.0.0.1:4173";
const outDir = path.resolve("ui-audit-artifacts");
await fs.mkdir(outDir, { recursive: true });

const ids = {
  faculty: "11111111-1111-1111-1111-111111111111",
  major: "22222222-2222-2222-2222-222222222222",
  program: "33333333-3333-3333-3333-333333333333",
  subject: "44444444-4444-4444-4444-444444444444",
  method: "55555555-5555-5555-5555-555555555555",
  cycle: "66666666-6666-6666-6666-666666666666",
};

const major = {
  id: ids.major,
  code: "7480201",
  name: "Công nghệ thông tin",
  facultyId: ids.faculty,
  facultyName: "Khoa Công nghệ thông tin",
  status: "active",
  description: "Chương trình đào tạo thực hành, định hướng công nghệ hiện đại.",
  careerOutcomes: "Kỹ sư phần mềm, chuyên viên dữ liệu, chuyên viên AI.",
  faculty: { id: ids.faculty, name: "Khoa Công nghệ thông tin" },
  programs: [
    {
      id: ids.program,
      code: "IT01",
      name: "Công nghệ thông tin",
      campus: "Hà Nội",
      description: "Chương trình đại học chính quy.",
      subjectCombinations: [{ id: ids.subject, code: "A00", subjects: "Toán, Vật lý, Hóa học" }],
      cutoffScores: [{ id: "77777777-7777-7777-7777-777777777777", year: 2026, methodCode: "CMC100", subjectCombinationCode: "A00", score: 24 }],
      tuitionFees: [{ id: "88888888-8888-8888-8888-888888888888", academicYear: "2026-2027", amountMin: 30000000, amountMax: 36000000, currency: "VND", unit: "năm" }],
    },
  ],
};

function json(body, status = 200) {
  return { status, contentType: "application/json", body: JSON.stringify(body) };
}

function responseFor(url, method) {
  const pathname = new URL(url).pathname;
  if (pathname === "/api/admissions/cycles") return json([{ id: ids.cycle, year: 2026, name: "Tuyển sinh 2026" }]);
  if (pathname === "/api/admissions/faculties") return json([{ id: ids.faculty, code: "FIT", name: "Khoa Công nghệ thông tin" }]);
  if (pathname === "/api/admissions/majors") return json({ items: [major], totalItems: 1, page: 1, pageSize: 50 });
  if (pathname === `/api/admissions/majors/${ids.major}`) return json(major);
  if (pathname === "/api/admissions/subject-combinations") return json([{ id: ids.subject, code: "A00", subjects: "Toán, Vật lý, Hóa học" }]);
  if (pathname === "/api/admissions/methods") return json([{ id: ids.method, code: "CMC100", name: "Xét điểm thi THPT" }]);
  if (pathname === "/api/admissions/faqs") return json([{ id: "faq-1", question: "Hồ sơ xét tuyển gồm những gì?", answer: "Thí sinh chuẩn bị thông tin cá nhân và minh chứng theo thông báo tuyển sinh." }]);
  if (pathname === "/api/admissions/compare-programs") return json({ summary: "So sánh 1 chương trình mẫu", items: [major.programs[0]] });
  if (pathname === "/api/chat/conversations") return json({ items: [{ id: "conv-1", title: "Học phí ngành CNTT", lastMessagePreview: "Học phí năm 2026" }], totalItems: 1 });
  if (pathname === "/api/chat/conversations/conv-1") return json({ id: "conv-1", messages: [{ id: "m1", role: "user", content: "Học phí ngành CNTT?" }, { id: "m2", role: "assistant", content: "Học phí dự kiến từ 30 đến 36 triệu đồng mỗi năm.", sources: [{ id: "s1", title: "Thông tin tuyển sinh 2026", pageNumber: 12, sectionTitle: "Học phí", score: 0.92, content: "Học phí chương trình Công nghệ thông tin dự kiến từ 30 đến 36 triệu đồng mỗi năm." }] }] });
  if (pathname === "/api/rag/chat") return json({ conversationId: "conv-1", assistantMessageId: "m2", answer: "Học phí dự kiến từ 30 đến 36 triệu đồng mỗi năm.", backend: "qdrant", sources: [{ id: "s1", title: "Thông tin tuyển sinh 2026", pageNumber: 12, sectionTitle: "Học phí", score: 0.92, content: "Học phí chương trình Công nghệ thông tin dự kiến từ 30 đến 36 triệu đồng mỗi năm." }] });
  if (pathname === "/api/auth/login") {
    return json({ accessToken: "audit-token", user: { id: "u1", fullName: "Nguyễn Minh Anh", email: "BIT240048@st.cmcu.edu.vn", roles: ["student"], status: "active" } });
  }
  if (pathname === "/api/auth/me") {
    return json({ id: "u1", fullName: "Nguyễn Minh Anh", email: "admin@example.com", roles: ["admin"], status: "active" });
  }
  if (pathname === "/api/profiles/me") return json({ user: { id: "u1", fullName: "Nguyễn Minh Anh", email: "BIT240048@st.cmcu.edu.vn", roles: ["student"], status: "active", phone: "0900000000" }, studentProfile: { province: "Hà Nội" } });
  if (pathname === "/api/admin/users") return json({ items: [{ id: "u1", fullName: "Nguyễn Minh Anh", email: "student@example.com", roles: ["student"], status: "active", createdAt: "2026-01-01T00:00:00Z" }], totalItems: 1, page: 1, totalPages: 1 });
  if (pathname === "/api/admin/documents") return json({ items: [{ id: "d1", title: "Thông tin tuyển sinh 2026", documentType: "regulation", status: "active", versions: [{ id: "v1", fileName: "tuyen-sinh-2026.pdf", processingStatus: "completed", chunkCount: 6 }] }], totalItems: 1 });
  if (pathname === "/api/admin/chat/feedback") return json({ items: [], totalItems: 0 });
  if (pathname === "/api/admin/handoff/tickets") return json({ items: [], totalItems: 0 });
  if (pathname === "/api/admin/dashboard") return json({ totalUsers: 25, totalDocuments: 4, completedDocumentVersions: 4, totalConversations: 18, totalChatMessages: 52, negativeFeedback: 2, openHandoffTickets: 1, resolvedHandoffTickets: 6, evaluationRuns: 3, latestEvaluationHitRateAtK: 0.9, latestEvaluationKeywordHitRate: 0.84, averageChatLatencyMs: 820 });
  if (pathname === "/api/admin/ai/status") return json({ aiServiceStatus: "ok", vectorBackend: "qdrant", qdrantAvailable: true, llmConfigured: true });
  if (pathname === "/api/admin/evaluation/questions") return json([{ id: "q1", code: "Q01", question: "Học phí ngành CNTT?" }]);
  if (pathname === "/api/admin/evaluation/runs") return json({ items: [], totalItems: 0 });
  if (pathname.includes("/chunks")) return json([{ id: "c1", chunkIndex: 0, sectionTitle: "Học phí", content: "Học phí chương trình Công nghệ thông tin từ 30 đến 36 triệu đồng mỗi năm." }]);
  if (method !== "GET") return json({ success: true, data: {} });
  return json({ success: true, data: {} });
}

async function installMocks(page) {
  await page.route("**/api/**", async (route) => {
    const request = route.request();
    await route.fulfill(responseFor(request.url(), request.method()));
  });
  await page.route("**/hubs/**", (route) => route.abort());
}

async function assertLayout(page, label) {
  const result = await page.evaluate(() => {
    const viewportWidth = document.documentElement.clientWidth;
    const overflow = document.documentElement.scrollWidth - viewportWidth;
    const visible = (el) => {
      const style = getComputedStyle(el);
      const rect = el.getBoundingClientRect();
      return style.display !== "none" && style.visibility !== "hidden" && Number(style.opacity) > 0 && rect.width > 0 && rect.height > 0;
    };
    const clippedButtons = [...document.querySelectorAll("button")]
      .filter(visible)
      .filter((el) => el.scrollWidth > el.clientWidth + 3 || el.scrollHeight > el.clientHeight + 3)
      .map((el) => el.textContent.trim().slice(0, 80));
    const tinyTargets = [...document.querySelectorAll("button, input:not([type='checkbox']):not([type='radio']), select, textarea")]
      .filter(visible)
      .map((el) => ({ text: (el.textContent || el.getAttribute("aria-label") || el.name || el.type || "control").trim().slice(0, 60), rect: el.getBoundingClientRect() }))
      .filter(({ rect }) => rect.width < 32 || rect.height < 32)
      .map(({ text, rect }) => `${text} (${Math.round(rect.width)}x${Math.round(rect.height)})`);
    const invalidFonts = [...document.querySelectorAll("body, h1, h2, h3, p, button, input, select, textarea")]
      .filter(visible)
      .filter((el) => !getComputedStyle(el).fontFamily)
      .length;
    const outsideViewport = [...document.querySelectorAll("button, input, select, textarea, h1, h2, h3")]
      .filter(visible)
      .map((el) => ({ text: (el.textContent || el.getAttribute("placeholder") || el.type || "element").trim().slice(0, 60), rect: el.getBoundingClientRect() }))
      .filter(({ rect }) => rect.left < -3 || rect.right > viewportWidth + 3)
      .map(({ text, rect }) => `${text} [${Math.round(rect.left)}, ${Math.round(rect.right)}]`);
    return { overflow, clippedButtons, tinyTargets, invalidFonts, outsideViewport };
  });
  if (result.overflow > 3) throw new Error(`${label}: horizontal overflow ${result.overflow}px`);
  if (result.clippedButtons.length) throw new Error(`${label}: clipped button text: ${result.clippedButtons.join(" | ")}`);
  if (result.tinyTargets.length) throw new Error(`${label}: controls below 32px: ${result.tinyTargets.join(" | ")}`);
  if (result.invalidFonts) throw new Error(`${label}: ${result.invalidFonts} elements have no computed font family`);
  if (result.outsideViewport.length) throw new Error(`${label}: controls/text outside viewport: ${result.outsideViewport.join(" | ")}`);
}

async function shot(page, name) {
  await page.screenshot({ path: path.join(outDir, `${name}.png`), fullPage: true });
}

async function openPortal(page) {
  await page.goto(baseURL, { waitUntil: "networkidle" });
  const guestButton = page.getByRole("button", { name: "Hỏi AI ngay", exact: true });
  if (await guestButton.isVisible()) {
    await guestButton.click();
  }
  await page.getByPlaceholder("Nhập câu hỏi tuyển sinh của bạn...").waitFor();
}

async function auditViewport(browser, viewport, suffix) {
  const context = await browser.newContext({ viewport });
  const page = await context.newPage();
  await installMocks(page);

  await page.goto(baseURL, { waitUntil: "networkidle" });
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await page.getByRole("heading", { name: "Đăng nhập sinh viên" }).waitFor();
  await assertLayout(page, `login-student-${suffix}`);
  await shot(page, `login-student-${suffix}`);
  await page.getByRole("button", { name: "Phụ huynh" }).click();
  await assertLayout(page, `login-parent-${suffix}`);
  await shot(page, `login-parent-${suffix}`);
  await page.getByRole("button", { name: "Cổng quản trị nhà trường" }).click();
  await assertLayout(page, `login-admin-${suffix}`);
  await shot(page, `login-admin-${suffix}`);

  await openPortal(page);
  for (const tab of ["Trợ lý AI", "Ngành đào tạo", "So sánh", "Câu hỏi thường gặp"]) {
    await page.getByRole("button", { name: new RegExp(`^${tab}`) }).first().click();
    await page.waitForTimeout(100);
    await assertLayout(page, `portal-${tab}-${suffix}`);
    await shot(page, `portal-${tab.toLowerCase().replaceAll(" ", "-")}-${suffix}`);
  }

  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await page.getByRole("button", { name: "Nguyễn Minh Anh", exact: true }).click();
  await assertLayout(page, `member-profile-${suffix}`);
  await shot(page, `member-profile-${suffix}`);

  await page.evaluate(() => {
    localStorage.setItem("admissions_token", "audit-token");
    localStorage.removeItem("admissions_member_token");
  });
  await page.reload({ waitUntil: "networkidle" });
  await page.getByRole("button", { name: "Quản trị", exact: true }).click();
  for (const tab of ["Tổng quan", "Tài khoản", "RAG & hỗ trợ", "Đánh giá", "Dữ liệu tuyển sinh"]) {
    await page.getByRole("button", { name: tab, exact: true }).click();
    await page.waitForTimeout(120);
    await assertLayout(page, `admin-${tab}-${suffix}`);
    await shot(page, `admin-${tab.toLowerCase().replaceAll(" ", "-").replaceAll("&", "and")}-${suffix}`);
  }
  await context.close();
}

const browser = await chromium.launch({ headless: true });
try {
  await auditViewport(browser, { width: 1440, height: 900 }, "desktop");
  await auditViewport(browser, { width: 390, height: 844 }, "mobile");
  console.log("UI audit passed for login, portal, member profile, and every admin tab on desktop and mobile.");
} finally {
  await browser.close();
}
