import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useEffect, useMemo, useRef, useState } from "react";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

const emptyMajor = {
  facultyId: "",
  code: "",
  name: "",
  description: "",
  careerOutcomes: "",
  status: "active",
};

const emptyProgram = {
  majorId: "",
  code: "",
  name: "",
  degreeType: "Đại học chính quy",
  language: "Tiếng Việt",
  campus: "Hà Nội",
  durationYears: 4,
  description: "",
  status: "active",
  subjectCombinationIds: [],
};

function todayYear() {
  return new Date().getFullYear();
}

function getClientSessionId() {
  const key = "admissions_client_session_id";
  const existing = localStorage.getItem(key);
  if (existing) return existing;
  const next = crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random()}`;
  localStorage.setItem(key, next);
  return next;
}

async function api(path, options = {}) {
  const isFormData = options.body instanceof FormData;
  const headers = {
    ...(isFormData ? {} : { "Content-Type": "application/json" }),
    ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
    ...options.headers,
  };
  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok || payload?.success === false) {
    const message = payload?.message || payload?.error?.message || response.statusText;
    throw new Error(message);
  }
  return payload?.data ?? payload;
}

function App() {
  const [view, setView] = useState("portal");
  const [authTarget, setAuthTarget] = useState("student");
  const [data, setData] = useState({
    cycles: [],
    faculties: [],
    majors: { items: [], totalItems: 0, page: 1, pageSize: 20 },
    subjects: [],
    methods: [],
    faqs: [],
  });
  const [filters, setFilters] = useState({
    keyword: "",
    facultyId: "",
    subjectCombinationCode: "",
    maxTuition: "",
    campus: "",
  });
  const [selectedMajorId, setSelectedMajorId] = useState("");
  const [selectedMajor, setSelectedMajor] = useState(null);
  const [selectedPrograms, setSelectedPrograms] = useState([]);
  const [comparison, setComparison] = useState(null);
  const [token, setToken] = useState(() => localStorage.getItem("admissions_token") ?? "");
  const [adminUser, setAdminUser] = useState(null);
  const [login, setLogin] = useState({ email: "admin@example.com", password: "Admin123456!" });
  const [memberToken, setMemberToken] = useState(() => localStorage.getItem("admissions_member_token") ?? "");
  const [memberUser, setMemberUser] = useState(null);
  const [memberMode, setMemberMode] = useState("student");
  const [memberAuthMode, setMemberAuthMode] = useState("login");
  const [memberForm, setMemberForm] = useState({
    email: "BIT240048@st.cmcu.edu.vn",
    password: "Student123456!",
    fullName: "Nguyễn Thu Hà",
    phone: "0900000000",
  });
  const [memberProfile, setMemberProfile] = useState(null);
  const [memberProfileForm, setMemberProfileForm] = useState({
    fullName: "",
    phone: "",
    occupation: "",
    province: "",
    contactPreference: "",
  });
  const [passwordForm, setPasswordForm] = useState({ currentPassword: "", newPassword: "" });
  const [adminForms, setAdminForms] = useState(() => initialAdminForms());
  const [users, setUsers] = useState({ items: [], totalItems: 0, page: 1, pageSize: 20, totalPages: 0 });
  const [userFilters, setUserFilters] = useState({ keyword: "", role: "", status: "" });
  const [documents, setDocuments] = useState({ items: [], totalItems: 0 });
  const [chatFeedbacks, setChatFeedbacks] = useState({ items: [], totalItems: 0 });
  const [chatFeedbackFilter, setChatFeedbackFilter] = useState("all");
  const [handoffTickets, setHandoffTickets] = useState({ items: [], totalItems: 0 });
  const [handoffReplies, setHandoffReplies] = useState({});
  const [handoffRealtimeStatus, setHandoffRealtimeStatus] = useState("offline");
  const [dashboard, setDashboard] = useState(null);
  const [aiStatus, setAiStatus] = useState(null);
  const [evaluationQuestions, setEvaluationQuestions] = useState([]);
  const [evaluationRuns, setEvaluationRuns] = useState({ items: [], totalItems: 0 });
  const [latestEvaluationRun, setLatestEvaluationRun] = useState(null);
  const evaluationAutomationToken = useRef("");
  const [documentUpload, setDocumentUpload] = useState({
    title: "",
    documentType: "regulation",
    source: "",
    file: null,
  });
  const [status, setStatus] = useState({ loading: true, message: "Đang tải dữ liệu...", error: "" });
  const [clientSessionId] = useState(() => getClientSessionId());
  const [chatConversations, setChatConversations] = useState({ items: [], totalItems: 0 });
  const [activeConversationId, setActiveConversationId] = useState("");
  const [chatMessages, setChatMessages] = useState([]);
  const [ragQuestion, setRagQuestion] = useState("");
  const [ragFile, setRagFile] = useState(null);
  const [ragChat, setRagChat] = useState(null);
  const [ragLoading, setRagLoading] = useState(false);

  const allPrograms = useMemo(() => {
    return data.majors.items.flatMap((major) =>
      major.programs.map((program) => ({
        ...program,
        majorName: major.name,
        majorCode: major.code,
      })),
    );
  }, [data.majors.items]);

  function getChatAccessToken() {
    return memberToken || token || "";
  }

  useEffect(() => {
    refreshAll();
    refreshChatConversations();
  }, []);

  useEffect(() => {
    if (token) {
      api("/api/auth/me", { token })
        .then((user) => {
          setAdminUser(user);
          refreshUsers(token);
          refreshDocuments(token);
          refreshChatFeedbacks(token);
          refreshHandoffTickets(token);
          if (evaluationAutomationToken.current !== token) {
            evaluationAutomationToken.current = token;
            ensureEvaluationReady(token).catch((error) => {
              setStatus({ loading: false, message: "", error: `Không thể tự động đánh giá RAG: ${error.message}` });
            });
          } else {
            refreshEvaluation(token);
          }
          refreshDashboard(token);
          refreshAiStatus(token);
        })
        .catch(() => {
          localStorage.removeItem("admissions_token");
          setToken("");
          setAdminUser(null);
        });
    }
  }, [token]);

  useEffect(() => {
    if (memberToken) {
      api("/api/auth/me", { token: memberToken })
        .then((user) => {
          setMemberUser(user);
          loadMemberProfile(memberToken);
        })
        .catch(() => {
          localStorage.removeItem("admissions_member_token");
          setMemberToken("");
          setMemberUser(null);
          setMemberProfile(null);
        });
    }
  }, [memberToken]);

  useEffect(() => {
    setActiveConversationId("");
    setChatMessages([]);
    setRagChat(null);
    refreshChatConversations();
  }, [token, memberToken]);

  useEffect(() => {
    if (!token || !adminUser) {
      setHandoffRealtimeStatus("offline");
      return undefined;
    }

    let disposed = false;
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/handoff`, {
        accessTokenFactory: () => token,
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("handoffTicketCreated", () => {
      refreshHandoffTickets(token);
      refreshDashboard(token);
    });

    connection.on("handoffTicketUpdated", () => {
      refreshHandoffTickets(token);
      refreshDashboard(token);
    });

    connection.onreconnecting(() => setHandoffRealtimeStatus("reconnecting"));
    connection.onreconnected(async () => {
      setHandoffRealtimeStatus("online");
      try {
        await connection.invoke("JoinStaffQueue");
      } catch {
        setHandoffRealtimeStatus("limited");
      }
    });
    connection.onclose(() => {
      if (!disposed) setHandoffRealtimeStatus("offline");
    });

    connection
      .start()
      .then(async () => {
        if (disposed || connection.state !== HubConnectionState.Connected) return;
        await connection.invoke("JoinStaffQueue");
        setHandoffRealtimeStatus("online");
      })
      .catch(() => {
        if (!disposed) setHandoffRealtimeStatus("offline");
      });

    return () => {
      disposed = true;
      setHandoffRealtimeStatus("offline");
      connection.stop();
    };
  }, [token, adminUser]);

  async function refreshAll(nextFilters = filters) {
    setStatus({ loading: true, message: "Đang tải dữ liệu...", error: "" });
    try {
      const query = new URLSearchParams({
        page: "1",
        pageSize: "50",
      });
      Object.entries(nextFilters).forEach(([key, value]) => {
        if (value) query.set(key, value);
      });
      const [cycles, faculties, majors, subjects, methods, faqs] = await Promise.all([
        api("/api/admissions/cycles"),
        api("/api/admissions/faculties"),
        api(`/api/admissions/majors?${query}`),
        api("/api/admissions/subject-combinations"),
        api("/api/admissions/methods"),
        api("/api/admissions/faqs"),
      ]);
      setData({ cycles, faculties, majors, subjects, methods, faqs });
      const currentStillVisible = majors.items?.some((major) => major.id === selectedMajorId);
      if (majors.items?.length && !currentStillVisible) {
        await loadMajor(majors.items[0].id);
      }
      if (!majors.items?.length) {
        setSelectedMajorId("");
        setSelectedMajor(null);
      }
      setStatus({ loading: false, message: "Dữ liệu đã cập nhật", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function loadMajor(id) {
    if (!id) return;
    setSelectedMajorId(id);
    const detail = await api(`/api/admissions/majors/${id}`);
    setSelectedMajor(detail);
  }

  async function comparePrograms() {
    if (selectedPrograms.length < 2) {
      setStatus({ loading: false, message: "", error: "Cần chọn ít nhất 2 chương trình để so sánh." });
      return;
    }
    try {
      const result = await api("/api/admissions/compare-programs", {
        method: "POST",
        body: JSON.stringify({ programIds: selectedPrograms }),
      });
      setComparison(result);
      setStatus({ loading: false, message: "Đã tạo bảng so sánh", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function submitLogin(event) {
    event.preventDefault();
    try {
      const result = await api("/api/auth/login", {
        method: "POST",
        body: JSON.stringify(login),
      });
      localStorage.setItem("admissions_token", result.accessToken);
      setToken(result.accessToken);
      setAdminUser(result.user);
      setView("admin");
      await refreshUsers(result.accessToken);
      await refreshDocuments(result.accessToken);
      await refreshChatFeedbacks(result.accessToken);
      await refreshHandoffTickets(result.accessToken);
      await refreshEvaluation(result.accessToken);
      await refreshDashboard(result.accessToken);
      await refreshAiStatus(result.accessToken);
      setStatus({ loading: false, message: "Đăng nhập thành công", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function submitMemberAuth(event) {
    event.preventDefault();
    try {
      if (memberMode === "student" && memberAuthMode === "register") {
        throw new Error("Tài khoản sinh viên do nhà trường cấp, không tự đăng ký trên cổng này.");
      }

      if (memberMode === "student" && !/^BIT\d{6}@st\.cmcu\.edu\.vn$/i.test(memberForm.email.trim())) {
        throw new Error("Email sinh viên phải có dạng BIT240048@st.cmcu.edu.vn.");
      }

      if (memberAuthMode === "register") {
        await api("/api/auth/register-parent", {
          method: "POST",
          body: JSON.stringify({
            email: memberForm.email,
            password: memberForm.password,
            fullName: memberForm.fullName,
            phone: memberForm.phone,
          }),
        });
      }

      const result = await api("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email: memberForm.email, password: memberForm.password }),
      });
      localStorage.setItem("admissions_member_token", result.accessToken);
      setMemberToken(result.accessToken);
      setMemberUser(result.user);
      setMemberMode(result.user.roles?.includes("parent") ? "parent" : "student");
      setView("portal");
      await loadMemberProfile(result.accessToken);
      setStatus({ loading: false, message: memberAuthMode === "register" ? "Đã tạo và đăng nhập tài khoản phụ huynh" : "Đăng nhập thành công", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  function logout() {
    localStorage.removeItem("admissions_token");
    setToken("");
    setAdminUser(null);
    setUsers({ items: [], totalItems: 0, page: 1, pageSize: 20, totalPages: 0 });
    setDocuments({ items: [], totalItems: 0 });
    setDocumentChunks([]);
    setChatFeedbacks({ items: [], totalItems: 0 });
    setHandoffTickets({ items: [], totalItems: 0 });
    setHandoffReplies({});
    setDashboard(null);
    setAiStatus(null);
    setEvaluationQuestions([]);
    setEvaluationRuns({ items: [], totalItems: 0 });
    setLatestEvaluationRun(null);
  }

  function logoutMember() {
    localStorage.removeItem("admissions_member_token");
    setMemberToken("");
    setMemberUser(null);
    setMemberProfile(null);
    setPasswordForm({ currentPassword: "", newPassword: "" });
  }

  async function loadMemberProfile(activeToken = memberToken) {
    if (!activeToken) return;
    const profile = await api("/api/profiles/me", { token: activeToken });
    setMemberProfile(profile);
    setMemberProfileForm({
      fullName: profile.user?.fullName ?? "",
      phone: profile.user?.phone ?? "",
      occupation: profile.parentProfile?.occupation ?? "",
      province: profile.parentProfile?.province ?? profile.studentProfile?.province ?? "",
      contactPreference: profile.parentProfile?.contactPreference ?? "",
    });
  }

  async function saveMemberProfile(event) {
    event.preventDefault();
    if (!memberToken) return;
    const isStudent = memberUser?.roles?.includes("student");
    const body = isStudent
      ? { phone: memberProfileForm.phone }
      : {
          fullName: memberProfileForm.fullName,
          phone: memberProfileForm.phone,
          parentProfile: {
            occupation: memberProfileForm.occupation,
            province: memberProfileForm.province,
            contactPreference: memberProfileForm.contactPreference,
          },
        };

    try {
      const profile = await api("/api/profiles/me", {
        method: "PUT",
        token: memberToken,
        body: JSON.stringify(body),
      });
      setMemberProfile(profile);
      if (profile.user) {
        setMemberUser(profile.user);
      }
      setStatus({ loading: false, message: "Đã cập nhật hồ sơ tài khoản", error: "" });
      await loadMemberProfile(memberToken);
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function changeMemberPassword(event) {
    event.preventDefault();
    if (!memberToken) return;
    try {
      await api("/api/auth/change-password", {
        method: "POST",
        token: memberToken,
        body: JSON.stringify(passwordForm),
      });
      setPasswordForm({ currentPassword: "", newPassword: "" });
      setStatus({ loading: false, message: "Đã đổi mật khẩu", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  function openAuth(role) {
    setAuthTarget(role);
    if (role === "student" || role === "parent") {
      setMemberMode(role);
      setMemberAuthMode("login");
      setMemberForm((current) => ({
        ...current,
        email: role === "student" ? "BIT240048@st.cmcu.edu.vn" : "phuhuynh@example.com",
        password: role === "student" ? "Student123456!" : current.password,
      }));
    }
    setView("login");
  }

  async function refreshDocuments(activeToken = token) {
    if (!activeToken) return;
    const result = await api("/api/admin/documents", { token: activeToken });
    setDocuments(result);
  }

  async function refreshUsers(activeToken = token, nextFilters = userFilters, page = 1) {
    if (!activeToken) return;
    const query = new URLSearchParams({
      page: String(page),
      pageSize: "20",
    });
    Object.entries(nextFilters).forEach(([key, value]) => {
      if (value) query.set(key, value);
    });
    const result = await api(`/api/admin/users?${query}`, { token: activeToken });
    setUsers(result);
  }

  async function updateUserStatus(userId, statusValue, nextFilters = userFilters, page = users.page || 1) {
    if (!token) return;
    try {
      await api(`/api/admin/users/${userId}/status`, {
        method: "PATCH",
        token,
        body: JSON.stringify({ status: statusValue, reason: "Cập nhật từ cổng quản trị" }),
      });
      await refreshUsers(token, nextFilters, page);
      await refreshDashboard(token);
      setStatus({ loading: false, message: "Đã cập nhật trạng thái tài khoản", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function refreshChatFeedbacks(activeToken = token, rating = chatFeedbackFilter) {
    if (!activeToken) return;
    const query = new URLSearchParams({
      page: "1",
      pageSize: "20",
    });
    if (rating && rating !== "all") {
      query.set("rating", rating);
    }
    const result = await api(`/api/admin/chat/feedback?${query.toString()}`, { token: activeToken });
    setChatFeedbacks(result);
  }

  async function refreshHandoffTickets(activeToken = token) {
    if (!activeToken) return;
    const result = await api("/api/admin/handoff/tickets?page=1&pageSize=20", { token: activeToken });
    setHandoffTickets(result);
  }

  async function refreshDashboard(activeToken = token) {
    if (!activeToken) return;
    const result = await api("/api/admin/dashboard", { token: activeToken });
    setDashboard(result);
  }

  async function refreshAiStatus(activeToken = token) {
    if (!activeToken) return;
    const result = await api("/api/admin/ai/status", { token: activeToken });
    setAiStatus(result);
  }

  async function refreshEvaluation(activeToken = token) {
    if (!activeToken) return;
    const [questions, runs] = await Promise.all([
      api("/api/admin/evaluation/questions?activeOnly=true", { token: activeToken }),
      api("/api/admin/evaluation/runs?page=1&pageSize=5", { token: activeToken }),
    ]);
    setEvaluationQuestions(questions);
    setEvaluationRuns(runs);
    const latestFinishedRun = runs.items?.find((run) => run.results?.length || run.status !== "running");
    if (latestFinishedRun?.id) {
      const detail = await api(`/api/admin/evaluation/runs/${latestFinishedRun.id}`, { token: activeToken });
      setLatestEvaluationRun(detail);
    } else {
      setLatestEvaluationRun(null);
    }
  }

  async function ensureEvaluationReady(activeToken = token, forceRun = false) {
    if (!activeToken) return;

    let questions = await api("/api/admin/evaluation/questions?activeOnly=true", { token: activeToken });
    if (questions.length === 0) {
      await api("/api/admin/evaluation/questions/seed-defaults", {
        method: "POST",
        token: activeToken,
      });
      questions = await api("/api/admin/evaluation/questions?activeOnly=true", { token: activeToken });
    }

    const runs = await api("/api/admin/evaluation/runs?page=1&pageSize=1", { token: activeToken });
    if (questions.length > 0 && (forceRun || !runs.items?.length)) {
      await api("/api/admin/evaluation/runs", {
        method: "POST",
        token: activeToken,
        body: JSON.stringify({ name: `Đánh giá tự động ${new Date().toLocaleString("vi-VN")}`, topK: 5 }),
      });
    }

    await refreshEvaluation(activeToken);
  }

  async function refreshChatConversations(activeToken = getChatAccessToken()) {
    const result = await api(`/api/chat/conversations?clientSessionId=${encodeURIComponent(clientSessionId)}&page=1&pageSize=20`, {
      token: activeToken || undefined,
    });
    setChatConversations(result);
  }

  async function loadChatConversation(id, activeToken = getChatAccessToken()) {
    if (!id) return;
    const result = await api(`/api/chat/conversations/${id}?clientSessionId=${encodeURIComponent(clientSessionId)}`, {
      token: activeToken || undefined,
    });
    setActiveConversationId(result.id);
    setChatMessages(result.messages);
    setRagChat(null);
  }

  function startNewChat() {
    setActiveConversationId("");
    setChatMessages([]);
    setRagChat(null);
    setRagQuestion("");
    setRagFile(null);
  }

  async function uploadDocument(event) {
    event.preventDefault();
    if (!documentUpload.file) {
      setStatus({ loading: false, message: "", error: "Cần chọn tệp tài liệu." });
      return;
    }

    try {
      setStatus({ loading: true, message: "Đang tải tài liệu lên...", error: "" });
      const form = new FormData();
      form.append("title", documentUpload.title || documentUpload.file.name);
      form.append("documentType", documentUpload.documentType);
      form.append("source", documentUpload.source);
      form.append("processNow", "true");
      form.append("file", documentUpload.file);
      await api("/api/admin/documents", {
        method: "POST",
        token,
        body: form,
      });
      setDocumentUpload({
        title: "",
        documentType: "regulation",
        source: "",
        file: null,
      });
      await refreshDocuments();
      await ensureEvaluationReady(token, true);
      setStatus({ loading: false, message: "Đã tải, xử lý tài liệu và đánh giá lại RAG", error: "" });
    } catch (error) {
      await refreshDocuments();
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function askRag(event) {
    event.preventDefault();
    const question = ragQuestion.trim();
    if (!question) {
      setRagChat({ error: "Cần nhập câu hỏi trước khi hỏi trợ lý." });
      return;
    }

    setRagLoading(true);
    setRagChat(null);
    const chatAccessToken = getChatAccessToken();
    const pendingUserMessage = { id: `pending-${Date.now()}`, role: "user", content: question, sources: [] };
    setChatMessages((current) => [...current, pendingUserMessage]);
    try {
      if (ragFile) {
        const form = new FormData();
        form.append("question", question);
        form.append("clientSessionId", clientSessionId);
        if (activeConversationId) form.append("conversationId", activeConversationId);
        form.append("file", ragFile);
        const detail = await api("/api/chat/conversations/file-question", {
          method: "POST",
          token: chatAccessToken || undefined,
          body: form,
        });
        setActiveConversationId(detail.id);
        setChatMessages(detail.messages);
        setRagFile(null);
        setRagQuestion("");
        refreshChatConversations(chatAccessToken);
        setStatus({ loading: false, message: "Đã hỏi theo tệp vừa tải lên", error: "" });
        return;
      }

      const result = await api("/api/rag/chat", {
        method: "POST",
        token: chatAccessToken || undefined,
        body: JSON.stringify({
          question,
          topK: 5,
          conversationId: activeConversationId || null,
          clientSessionId,
        }),
      });
      setRagChat(result);
      setActiveConversationId(result.conversationId ?? "");
      if (result.conversationId) {
        const detail = await api(`/api/chat/conversations/${result.conversationId}?clientSessionId=${encodeURIComponent(clientSessionId)}`, {
          token: chatAccessToken || undefined,
        });
        setChatMessages(detail.messages);
      }
      refreshChatConversations(chatAccessToken);
      setRagQuestion("");
      setStatus({ loading: false, message: "Đã truy vấn kho tài liệu RAG", error: "" });
    } catch (error) {
      setRagChat({ error: error.message });
      setStatus({ loading: false, message: "", error: error.message });
    } finally {
      setRagLoading(false);
    }
  }

  async function submitRagFeedback(rating, note = "") {
    if (!ragChat?.assistantMessageId) {
      setStatus({ loading: false, message: "", error: "Chưa có câu trả lời để đánh giá." });
      return false;
    }

    try {
      const trimmedNote = note.trim();
      const feedback = await api(`/api/chat/messages/${ragChat.assistantMessageId}/feedback`, {
        method: "POST",
        body: JSON.stringify({ rating, note: trimmedNote }),
      });
      setRagChat((current) => (
        current
          ? {
              ...current,
              feedbackSubmitted: rating,
              feedbackNote: trimmedNote,
              handoffTicketId: feedback.handoffTicketId,
            }
          : current
      ));
      setStatus({
        loading: false,
        message: feedback.handoffTicketId ? "Đã tạo phiếu để tư vấn viên phản hồi" : "Đã lưu đánh giá câu trả lời",
        error: "",
      });
      if (token) {
        await refreshChatFeedbacks();
        await refreshHandoffTickets();
      }
      return true;
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
      return false;
    }
  }

  async function submitAdminForm(kind, path, bodyBuilder) {
    try {
      const payload = bodyBuilder();
      await api(path, {
        method: "POST",
        token,
        body: JSON.stringify(payload),
      });
      setAdminForms((current) => resetAdminForm(current, kind));
      await refreshAll();
      setStatus({ loading: false, message: "Đã lưu dữ liệu quản trị", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function replyHandoffTicket(ticketId, resolve = false) {
    const content = handoffReplies[ticketId]?.trim();
    if (!content) {
      setStatus({ loading: false, message: "", error: "Cần nhập nội dung phản hồi." });
      return;
    }

    try {
      await api(`/api/admin/handoff/tickets/${ticketId}/reply`, {
        method: "POST",
        token,
        body: JSON.stringify({ content, resolve }),
      });
      setHandoffReplies((current) => ({ ...current, [ticketId]: "" }));
      await refreshHandoffTickets();
      setStatus({ loading: false, message: resolve ? "Đã phản hồi và đóng phiếu" : "Đã lưu phản hồi của tư vấn viên", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  async function updateHandoffStatus(ticketId, statusValue) {
    try {
      await api(`/api/admin/handoff/tickets/${ticketId}/status`, {
        method: "PATCH",
        token,
        body: JSON.stringify({ status: statusValue }),
      });
      await refreshHandoffTickets();
      setStatus({ loading: false, message: "Đã cập nhật trạng thái phiếu", error: "" });
    } catch (error) {
      setStatus({ loading: false, message: "", error: error.message });
    }
  }

  return (
    <main className="app-shell">
      {view === "login" ? (
        <AuthLanding
          authTarget={authTarget}
          login={login}
          setLogin={setLogin}
          memberForm={memberForm}
          setMemberForm={setMemberForm}
          memberMode={memberMode}
          setMemberMode={setMemberMode}
          memberAuthMode={memberAuthMode}
          setMemberAuthMode={setMemberAuthMode}
          onAdminLogin={submitLogin}
          onMemberSubmit={submitMemberAuth}
          onGuest={() => setView("portal")}
          status={status}
        />
      ) : (
        <>
      <Header
        view={view}
        setView={setView}
        status={status}
        adminUser={adminUser}
        memberUser={memberUser}
        onOpenAuth={openAuth}
        onLogout={logout}
        onMemberLogout={logoutMember}
      />
      {view === "portal" ? (
        <PortalView
          memberUser={memberUser}
          data={data}
          filters={filters}
          setFilters={setFilters}
          onSearch={() => refreshAll(filters)}
          selectedMajor={selectedMajor}
          selectedMajorId={selectedMajorId}
          onSelectMajor={loadMajor}
          selectedPrograms={selectedPrograms}
          setSelectedPrograms={setSelectedPrograms}
          allPrograms={allPrograms}
          comparison={comparison}
          onCompare={comparePrograms}
          ragQuestion={ragQuestion}
          setRagQuestion={setRagQuestion}
          ragFile={ragFile}
          setRagFile={setRagFile}
          ragChat={ragChat}
          conversations={chatConversations}
          activeConversationId={activeConversationId}
          messages={chatMessages}
          ragLoading={ragLoading}
          onAskRag={askRag}
          onFeedback={submitRagFeedback}
          onSelectConversation={loadChatConversation}
          onNewConversation={startNewChat}
        />
      ) : view === "member" ? (
        <MemberView
          memberUser={memberUser}
          memberMode={memberMode}
          setMemberMode={setMemberMode}
          memberAuthMode={memberAuthMode}
          setMemberAuthMode={setMemberAuthMode}
          memberForm={memberForm}
          setMemberForm={setMemberForm}
          memberProfile={memberProfile}
          profileForm={memberProfileForm}
          setProfileForm={setMemberProfileForm}
          passwordForm={passwordForm}
          setPasswordForm={setPasswordForm}
          onSubmit={submitMemberAuth}
          onSaveProfile={saveMemberProfile}
          onChangePassword={changeMemberPassword}
          onOpenAuth={openAuth}
          onLogout={logoutMember}
        />
      ) : (
        <AdminView
          token={token}
          adminUser={adminUser}
          status={status}
          login={login}
          setLogin={setLogin}
          onLogin={submitLogin}
          forms={adminForms}
          setForms={setAdminForms}
          data={data}
          users={users}
          userFilters={userFilters}
          setUserFilters={setUserFilters}
          dashboard={dashboard}
          aiStatus={aiStatus}
          documents={documents}
          chatFeedbacks={chatFeedbacks}
          chatFeedbackFilter={chatFeedbackFilter}
          handoffTickets={handoffTickets}
          handoffReplies={handoffReplies}
          handoffRealtimeStatus={handoffRealtimeStatus}
          setHandoffReplies={setHandoffReplies}
          evaluationQuestions={evaluationQuestions}
          evaluationRuns={evaluationRuns}
          latestEvaluationRun={latestEvaluationRun}
          documentUpload={documentUpload}
          setDocumentUpload={setDocumentUpload}
          onUploadDocument={uploadDocument}
          onRefreshDocuments={() => refreshDocuments()}
          onRefreshUsers={(nextFilters = userFilters, page = 1) => refreshUsers(token, nextFilters, page)}
          onUpdateUserStatus={updateUserStatus}
          onRefreshChatFeedbacks={() => refreshChatFeedbacks()}
          onChangeChatFeedbackFilter={(nextRating) => {
            setChatFeedbackFilter(nextRating);
            return refreshChatFeedbacks(token, nextRating);
          }}
          onRefreshHandoffTickets={() => refreshHandoffTickets()}
          onRefreshDashboard={() => refreshDashboard()}
          onRefreshAiStatus={() => refreshAiStatus()}
          onRefreshEvaluation={() => refreshEvaluation()}
          onReplyHandoffTicket={replyHandoffTicket}
          onUpdateHandoffStatus={updateHandoffStatus}
          onSubmit={submitAdminForm}
        />
      )}
        </>
      )}
    </main>
  );
}

function AuthLanding({
  authTarget,
  login,
  setLogin,
  memberForm,
  setMemberForm,
  memberMode,
  setMemberMode,
  memberAuthMode,
  setMemberAuthMode,
  onAdminLogin,
  onMemberSubmit,
  onGuest,
  status,
}) {
  const [role, setRole] = useState(authTarget ?? "student");
  const isAdmin = role === "admin";
  const isStudent = role === "student";
  const submitHandler = isAdmin ? onAdminLogin : onMemberSubmit;

  useEffect(() => {
    setRole(authTarget ?? "student");
    if (authTarget === "student" || authTarget === "parent") {
      setMemberMode(authTarget);
      setMemberAuthMode("login");
    }
  }, [authTarget, setMemberAuthMode, setMemberMode]);

  function selectRole(nextRole) {
    setRole(nextRole);
    if (nextRole === "student" || nextRole === "parent") {
      setMemberMode(nextRole);
      setMemberAuthMode("login");
      setMemberForm((current) => ({
        ...current,
        email: nextRole === "student" ? "BIT240048@st.cmcu.edu.vn" : "phuhuynh@example.com",
        password: nextRole === "student" ? "Student123456!" : current.password,
      }));
    }
  }

  return (
    <section className="login-shell auth-shell">
      <div className="auth-brand">
        <p className="eyebrow">Trường Đại học CMC</p>
        <h1>Tư vấn tuyển sinh CMCU</h1>
        <p>Chọn đúng cổng để bắt đầu. Khách vãng lai có thể hỏi AI ngay; sinh viên dùng tài khoản do nhà trường cấp; phụ huynh có tài khoản riêng để lưu lịch sử tư vấn.</p>
        <div className="auth-primary-actions">
          <button className="primary-button" type="button" onClick={onGuest}>Hỏi AI ngay</button>
          <button className="ghost-button" type="button" onClick={() => selectRole("student")}>Đăng nhập người học</button>
        </div>
      </div>

      <div className="auth-card">
        <div className="auth-tabs" aria-label="Chọn loại tài khoản">
          <button className={role === "student" ? "active" : ""} type="button" onClick={() => selectRole("student")}>Sinh viên</button>
          <button className={role === "parent" ? "active" : ""} type="button" onClick={() => selectRole("parent")}>Phụ huynh</button>
        </div>

        {!isAdmin ? (
          <form className="auth-form" onSubmit={submitHandler}>
            <div>
              <h2>{isStudent ? "Đăng nhập sinh viên" : memberAuthMode === "register" ? "Tạo tài khoản phụ huynh" : "Đăng nhập phụ huynh"}</h2>
              <p>
                {isStudent
                  ? "Tài khoản sinh viên do nhà trường cấp. Email có dạng BIT240048@st.cmcu.edu.vn."
                  : "Phụ huynh có thể đăng nhập hoặc tạo tài khoản để lưu lịch sử tư vấn."}
              </p>
            </div>

            {!isStudent ? (
              <div className="segmented small">
                <button className={memberAuthMode === "login" ? "active" : ""} type="button" onClick={() => setMemberAuthMode("login")}>Đăng nhập</button>
                <button className={memberAuthMode === "register" ? "active" : ""} type="button" onClick={() => setMemberAuthMode("register")}>Đăng ký</button>
              </div>
            ) : null}

            {memberAuthMode === "register" && !isStudent ? (
              <>
                <Field label="Họ tên phụ huynh" value={memberForm.fullName} onChange={(value) => setMemberForm({ ...memberForm, fullName: value })} />
                <Field label="Số điện thoại" value={memberForm.phone} onChange={(value) => setMemberForm({ ...memberForm, phone: value })} />
              </>
            ) : null}
            <Field label={isStudent ? "Email sinh viên" : "Email phụ huynh"} value={memberForm.email} onChange={(value) => setMemberForm({ ...memberForm, email: value })} />
            <Field label="Mật khẩu" type="password" value={memberForm.password} onChange={(value) => setMemberForm({ ...memberForm, password: value })} />
            <button className="primary-button" type="submit">
              {memberAuthMode === "register" && !isStudent ? "Tạo tài khoản phụ huynh" : "Đăng nhập"}
            </button>
          </form>
        ) : null}

        <div className="admin-entry">
          {isAdmin ? (
            <form className="auth-form" onSubmit={submitHandler}>
              <div>
                <h2>Cổng quản trị nhà trường</h2>
                <p>Chỉ dành cho cán bộ tuyển sinh, quản trị dữ liệu, tài liệu RAG và phiếu hỗ trợ.</p>
              </div>
              <Field label="Email quản trị" value={login.email} onChange={(value) => setLogin({ ...login, email: value })} />
              <Field label="Mật khẩu" type="password" value={login.password} onChange={(value) => setLogin({ ...login, password: value })} />
              <button className="primary-button" type="submit">Vào cổng quản trị</button>
              <button className="ghost-button" type="button" onClick={() => selectRole("student")}>Quay lại người học</button>
            </form>
          ) : (
            <button className="admin-link" type="button" onClick={() => selectRole("admin")}>
              Cổng quản trị nhà trường
            </button>
          )}
        </div>
        {status.error ? <p className="rag-error">{status.error}</p> : null}
      </div>
    </section>
  );
}

function Header({ view, setView, status, adminUser, memberUser, onOpenAuth, onLogout, onMemberLogout }) {
  const isPortal = view === "portal";
  const isMember = view === "member";
  const isAdmin = view === "admin";

  return (
    <header className="topbar">
      <div>
        <p className="eyebrow">Trường Đại học CMC</p>
        <h1>{isAdmin ? "Cổng quản trị tuyển sinh" : isMember ? "Hồ sơ tài khoản" : "Trợ lý tuyển sinh Đại học CMC"}</h1>
      </div>
      <div className="topbar-actions">
        {isPortal && !memberUser ? (
          <>
            <button className="ghost-button" onClick={() => onOpenAuth("student")}>Đăng nhập người học</button>
            <button className="ghost-button" onClick={() => onOpenAuth("admin")}>Cổng quản trị</button>
          </>
        ) : null}
        {isPortal && memberUser ? (
          <>
            <span className="account-chip">{memberUser.fullName}</span>
            <button className="ghost-button" onClick={() => setView("member")}>Hồ sơ</button>
          </>
        ) : null}
        {isMember ? <button className="ghost-button" onClick={() => setView("portal")}>Hỏi AI</button> : null}
        {isAdmin ? <button className="ghost-button" onClick={() => setView("portal")}>Xem cổng tư vấn</button> : null}
        {memberUser ? (
          <button className="ghost-button" onClick={onMemberLogout}>
            Thoát tài khoản
          </button>
        ) : null}
        {adminUser ? (
          <button className="ghost-button" onClick={onLogout}>
            Đăng xuất
          </button>
        ) : null}
      </div>
      <div className={`status-line ${status.error ? "error" : ""}`}>
        {status.error || status.message || "Sẵn sàng"}
      </div>
    </header>
  );
}

function PortalView({
  memberUser,
  data,
  filters,
  setFilters,
  onSearch,
  selectedMajor,
  selectedMajorId,
  onSelectMajor,
  selectedPrograms,
  setSelectedPrograms,
  allPrograms,
  comparison,
  onCompare,
  ragQuestion,
  setRagQuestion,
  ragFile,
  setRagFile,
  ragChat,
  conversations,
  activeConversationId,
  messages,
  ragLoading,
  onAskRag,
  onFeedback,
  onSelectConversation,
  onNewConversation,
}) {
  const [portalTab, setPortalTab] = useState("chat");
  const portalItems = [
    { id: "chat", label: "Trợ lý AI", description: "Hỏi đáp tuyển sinh" },
    { id: "majors", label: "Ngành đào tạo", description: "Tra cứu chương trình" },
    { id: "compare", label: "So sánh", description: "Đối chiếu học phí" },
    { id: "faq", label: "FAQ", description: "Câu hỏi thường gặp" },
  ];

  return (
    <div className="portal-shell">
      <aside className="portal-rail">
        <div className="portal-rail-head">
          <p className="eyebrow">CMCU Portal</p>
          <h2>{memberUser ? "Không gian tư vấn của bạn" : "Cổng tư vấn tuyển sinh"}</h2>
          <p>{memberUser ? `${memberUser.fullName} đang đăng nhập.` : "Hỏi AI trước, tra cứu chi tiết khi cần."}</p>
        </div>

        <nav className="portal-service-nav" aria-label="Dịch vụ tư vấn">
          {portalItems.map((item) => (
            <button key={item.id} className={portalTab === item.id ? "active" : ""} type="button" onClick={() => setPortalTab(item.id)}>
              <strong>{item.label}</strong>
              <span>{item.description}</span>
            </button>
          ))}
        </nav>

        <div className="portal-mini-stats">
          <Metric label="Ngành" value={data.majors.totalItems ?? data.majors.items.length} />
          <Metric label="Khoa" value={data.faculties.length} />
        </div>
      </aside>

      <section className="portal-workspace">
        <div className="portal-workspace-head">
          <div>
            <p className="eyebrow">{portalTab === "chat" ? "Trợ lý tuyển sinh" : "Dịch vụ tra cứu"}</p>
            <h2>{portalItems.find((item) => item.id === portalTab)?.label}</h2>
          </div>
          <span className="pill">{memberUser ? "Đã đăng nhập" : "Khách vãng lai"}</span>
        </div>

        {portalTab === "chat" ? (
          <RagChatPanel
            question={ragQuestion}
            setQuestion={setRagQuestion}
            file={ragFile}
            setFile={setRagFile}
            chat={ragChat}
            conversations={conversations}
            activeConversationId={activeConversationId}
            messages={messages}
            loading={ragLoading}
            onSubmit={onAskRag}
            onFeedback={onFeedback}
            onSelectConversation={onSelectConversation}
            onNewConversation={onNewConversation}
          />
        ) : null}

        {portalTab === "majors" ? (
          <MajorsExplorer
            data={data}
            filters={filters}
            setFilters={setFilters}
            onSearch={onSearch}
            selectedMajor={selectedMajor}
            selectedMajorId={selectedMajorId}
            onSelectMajor={onSelectMajor}
          />
        ) : null}

        {portalTab === "compare" ? (
          <ComparePanel
            programs={allPrograms}
            selectedPrograms={selectedPrograms}
            setSelectedPrograms={setSelectedPrograms}
            comparison={comparison}
            onCompare={onCompare}
          />
        ) : null}

        {portalTab === "faq" ? <FaqPanel faqs={data.faqs} /> : null}
      </section>
    </div>
  );
}

function MajorsExplorer({ data, filters, setFilters, onSearch, selectedMajor, selectedMajorId, onSelectMajor }) {
  return (
    <div className="major-explorer">
      <section className="filter-panel">
        <div className="section-title">
          <div>
            <h2>Bộ lọc ngành</h2>
            <p>Lọc theo khoa, tổ hợp, cơ sở hoặc mức học phí.</p>
          </div>
          <button className="primary-button compact" onClick={onSearch}>
            Tìm kiếm
          </button>
        </div>
        <div className="filter-grid">
          <label>
            Từ khóa
            <input value={filters.keyword} onChange={(event) => setFilters({ ...filters, keyword: event.target.value })} placeholder="Ví dụ: AI, dữ liệu, marketing" />
          </label>
          <label>
            Khoa
            <select value={filters.facultyId} onChange={(event) => setFilters({ ...filters, facultyId: event.target.value })}>
              <option value="">Tất cả khoa</option>
              {data.faculties.map((faculty) => (
                <option key={faculty.id} value={faculty.id}>{faculty.name}</option>
              ))}
            </select>
          </label>
          <label>
            Tổ hợp môn
            <select value={filters.subjectCombinationCode} onChange={(event) => setFilters({ ...filters, subjectCombinationCode: event.target.value })}>
              <option value="">Tất cả tổ hợp</option>
              {data.subjects.map((subject) => (
                <option key={subject.id} value={subject.code}>{subject.code} - {subject.subjects}</option>
              ))}
            </select>
          </label>
          <label>
            Học phí tối đa
            <input type="number" value={filters.maxTuition} onChange={(event) => setFilters({ ...filters, maxTuition: event.target.value })} placeholder="30000000" />
          </label>
          <label>
            Cơ sở
            <input value={filters.campus} onChange={(event) => setFilters({ ...filters, campus: event.target.value })} placeholder="Hà Nội, TP.HCM" />
          </label>
        </div>
      </section>

      <div className="split">
        <section className="list-pane">
          <div className="section-title">
            <h2>Danh sách ngành</h2>
            <span>{data.majors.items.length} kết quả</span>
          </div>
          <div className="major-list">
            {data.majors.items.map((major) => (
              <button key={major.id} className={`major-row ${selectedMajorId === major.id ? "selected" : ""}`} onClick={() => onSelectMajor(major.id)}>
                <span>
                  <strong>{major.name}</strong>
                  <small>{major.code} - {major.facultyName}</small>
                </span>
                <span className="pill">{major.programs.length} CT</span>
              </button>
            ))}
          </div>
        </section>
        <section className="detail-pane">
          {selectedMajor ? <MajorDetail major={selectedMajor} /> : <EmptyState text="Chọn một ngành để xem chi tiết." />}
        </section>
      </div>
    </div>
  );
}

function FaqPanel({ faqs }) {
  return (
    <section className="faq-panel">
      <div className="section-title">
        <div>
          <h2>Câu hỏi thường gặp</h2>
          <p>Những câu hỏi cơ bản về hồ sơ, học phí, phương thức và lịch tuyển sinh.</p>
        </div>
      </div>
      <div className="faq-list">
        {faqs.map((faq) => (
          <details key={faq.id}>
            <summary>{faq.question}</summary>
            <p>{faq.answer}</p>
          </details>
        ))}
      </div>
    </section>
  );
}

function MemberView({
  memberUser,
  memberMode,
  setMemberMode,
  memberAuthMode,
  setMemberAuthMode,
  memberForm,
  setMemberForm,
  memberProfile,
  profileForm,
  setProfileForm,
  passwordForm,
  setPasswordForm,
  onSubmit,
  onSaveProfile,
  onChangePassword,
  onOpenAuth,
  onLogout,
}) {
  const isStudent = memberUser?.roles?.includes("student") || memberMode === "student";
  const isParent = memberUser?.roles?.includes("parent") || memberMode === "parent";

  if (!memberUser) {
    return (
      <section className="member-shell">
        <div className="member-empty">
          <p className="eyebrow">Tài khoản người học</p>
          <h2>Đăng nhập để xem hồ sơ riêng</h2>
          <p>Sinh viên dùng tài khoản do nhà trường cấp. Phụ huynh có thể đăng nhập hoặc tạo tài khoản để lưu lịch sử tư vấn.</p>
          <div className="login-actions">
            <button className="primary-button" type="button" onClick={() => onOpenAuth("student")}>Đăng nhập sinh viên</button>
            <button className="ghost-button" type="button" onClick={() => onOpenAuth("parent")}>Đăng nhập phụ huynh</button>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="member-shell">
      <section className="member-profile-grid">
        <aside className="member-card identity-card">
          <p className="eyebrow">{isStudent ? "Tài khoản sinh viên" : "Tài khoản phụ huynh"}</p>
          <h2>{memberUser.fullName}</h2>
          <p>{memberUser.email}</p>
          <div className="account-summary compact">
            <Metric label="Vai trò" value={isStudent ? "Sinh viên" : isParent ? "Phụ huynh" : memberUser.roles?.join(", ") || "-"} />
            <Metric label="Trạng thái" value={statusLabel(memberUser.status)} />
          </div>
          {isStudent ? (
            <p className="hint-text">Họ tên và mã sinh viên do nhà trường quản lý. Sinh viên chỉ tự cập nhật số điện thoại và mật khẩu.</p>
          ) : (
            <p className="hint-text">Phụ huynh có thể cập nhật thông tin liên hệ để tư vấn viên hỗ trợ thuận tiện hơn.</p>
          )}
          <button className="ghost-button" type="button" onClick={onLogout}>Đăng xuất</button>
        </aside>

        <form className="member-card member-form" onSubmit={onSaveProfile}>
          <div className="section-title">
            <div>
              <h2>Thông tin tài khoản</h2>
              <p>{isStudent ? "Thông tin sinh viên được khóa theo dữ liệu nhà trường." : "Cập nhật hồ sơ phụ huynh dùng cho tư vấn tuyển sinh."}</p>
            </div>
          </div>
          <div className="form-grid">
            <Field label="Email" value={memberUser.email} disabled />
            <Field label="Họ tên" value={isStudent ? memberUser.fullName : profileForm.fullName} disabled={isStudent} onChange={(value) => setProfileForm({ ...profileForm, fullName: value })} />
            <Field label="Số điện thoại" value={profileForm.phone} onChange={(value) => setProfileForm({ ...profileForm, phone: value })} />
            {!isStudent ? (
              <>
                <Field label="Nghề nghiệp" value={profileForm.occupation} onChange={(value) => setProfileForm({ ...profileForm, occupation: value })} />
                <Field label="Tỉnh/thành" value={profileForm.province} onChange={(value) => setProfileForm({ ...profileForm, province: value })} />
                <Field label="Cách liên hệ mong muốn" value={profileForm.contactPreference} onChange={(value) => setProfileForm({ ...profileForm, contactPreference: value })} />
              </>
            ) : null}
          </div>
          <button className="primary-button" type="submit">Lưu thông tin</button>
        </form>

        <form className="member-card member-form" onSubmit={onChangePassword}>
          <div className="section-title">
            <div>
              <h2>Đổi mật khẩu</h2>
              <p>Đổi mật khẩu định kỳ để bảo vệ tài khoản cá nhân.</p>
            </div>
          </div>
          <div className="form-grid">
            <Field label="Mật khẩu hiện tại" type="password" value={passwordForm.currentPassword} onChange={(value) => setPasswordForm({ ...passwordForm, currentPassword: value })} />
            <Field label="Mật khẩu mới" type="password" value={passwordForm.newPassword} onChange={(value) => setPasswordForm({ ...passwordForm, newPassword: value })} />
          </div>
          <button className="primary-button" type="submit">Đổi mật khẩu</button>
        </form>
      </section>
    </section>
  );
}

function MajorDetail({ major }) {
  return (
    <article>
      <div className="section-title">
        <div>
          <h2>{major.name}</h2>
          <p>{major.code} - {major.faculty.name}</p>
        </div>
        <span className="pill">{statusLabel(major.status)}</span>
      </div>
      <p className="description">{major.description || "Chưa có mô tả."}</p>
      <div className="detail-grid">
        <div>
          <h3>Cơ hội nghề nghiệp</h3>
          <p>{major.careerOutcomes || "Chưa cập nhật."}</p>
        </div>
        <div>
          <h3>Chương trình</h3>
          <div className="program-stack">
            {major.programs.map((program) => (
              <div className="program-box" key={program.id}>
                <div className="program-head">
                  <strong>{program.name}</strong>
                  <span>{program.campus || "Chưa rõ cơ sở"}</span>
                </div>
                <p>{program.description}</p>
                <div className="tag-line">
                  {program.subjectCombinations.map((subject) => (
                    <span className="tag" key={subject.id}>
                      {subject.code}
                    </span>
                  ))}
                </div>
                <table>
                  <thead>
                    <tr>
                      <th>Năm</th>
                      <th>Phương thức</th>
                      <th>Tổ hợp</th>
                      <th>Điểm</th>
                    </tr>
                  </thead>
                  <tbody>
                    {program.cutoffScores.map((score) => (
                      <tr key={score.id}>
                        <td>{score.year}</td>
                        <td>{score.methodCode}</td>
                        <td>{score.subjectCombinationCode || "-"}</td>
                        <td>{score.score}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <div className="tuition-line">
                  {program.tuitionFees.map((fee) => (
                    <span key={fee.id}>
                      {fee.academicYear}: {money(fee.amountMin)} - {money(fee.amountMax)} {fee.currency}/{fee.unit}
                    </span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </article>
  );
}

function ComparePanel({ programs, selectedPrograms, setSelectedPrograms, comparison, onCompare }) {
  function toggleProgram(id) {
    setSelectedPrograms((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id].slice(-4),
    );
  }

  return (
    <section className="compare-panel">
      <div className="section-title">
        <h2>So sánh chương trình</h2>
        <button className="primary-button compact" onClick={onCompare}>
          So sánh
        </button>
      </div>
      <div className="choice-grid">
        {programs.map((program) => (
          <label className="choice" key={program.id}>
            <input
              type="checkbox"
              checked={selectedPrograms.includes(program.id)}
              onChange={() => toggleProgram(program.id)}
            />
            <span>
              <strong>{program.name}</strong>
              <small>{program.majorName}</small>
            </span>
          </label>
        ))}
      </div>
      {comparison ? (
        <div className="comparison-table">
          <p>{comparison.summary}</p>
          <table>
            <thead>
              <tr>
                <th>Chương trình</th>
                <th>Tổ hợp</th>
                <th>Điểm gần nhất</th>
                <th>Học phí</th>
              </tr>
            </thead>
            <tbody>
              {comparison.items.map((program) => (
                <tr key={program.id}>
                  <td>{program.name}</td>
                  <td>{program.subjectCombinations.map((item) => item.code).join(", ")}</td>
                  <td>{program.cutoffScores[0]?.score ?? "-"}</td>
                  <td>{program.tuitionFees[0] ? `${money(program.tuitionFees[0].amountMin)} - ${money(program.tuitionFees[0].amountMax)}` : "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  );
}

function RagChatPanel({
  question,
  setQuestion,
  file,
  setFile,
  chat,
  conversations,
  activeConversationId,
  messages,
  loading,
  onSubmit,
  onFeedback,
  onSelectConversation,
  onNewConversation,
}) {
  const [negativeFeedbackOpen, setNegativeFeedbackOpen] = useState(false);
  const [negativeFeedbackNote, setNegativeFeedbackNote] = useState("");
  const visibleMessages = messages?.length
    ? messages
    : chat?.answer
      ? [{ id: chat.assistantMessageId ?? "latest", role: "assistant", content: chat.answer, sources: chat.sources ?? [] }]
      : [];
  const displayMessages = loading
    ? [...visibleMessages, { id: "thinking", role: "assistant", content: "Đang tìm nguồn phù hợp và tạo câu trả lời...", sources: [] }]
    : visibleMessages;
  const feedbackLocked = Boolean(chat?.feedbackSubmitted);

  useEffect(() => {
    setNegativeFeedbackOpen(false);
    setNegativeFeedbackNote("");
  }, [chat?.assistantMessageId]);

  async function handlePositiveFeedback() {
    const saved = await onFeedback("positive");
    if (saved) {
      setNegativeFeedbackOpen(false);
      setNegativeFeedbackNote("");
    }
  }

  async function handleNegativeFeedback() {
    if (!negativeFeedbackNote.trim()) {
      return;
    }

    const saved = await onFeedback("negative", negativeFeedbackNote);
    if (saved) {
      setNegativeFeedbackOpen(false);
      setNegativeFeedbackNote("");
    }
  }

  return (
    <section className="rag-chat-panel">
      <div className="section-title">
        <div>
          <h2>Trợ lý RAG tuyển sinh CMCU</h2>
          <p>Trả lời dựa trên PDF, DOCX, ảnh và tài liệu tuyển sinh Đại học CMC đã nạp trong kho quản trị.</p>
        </div>
        {chat?.backend ? <span className="pill">{chat.backend}</span> : null}
      </div>

      <div className="question-suggestions">
        {[
          "Trường thành lập năm nào?",
          "Hồ sơ xét tuyển trực tuyến gồm những gì?",
          "Học phí ngành Trí tuệ Nhân tạo là bao nhiêu?",
          "Đại học CMC có những phương thức xét tuyển nào?",
        ].map((suggestion) => (
          <button key={suggestion} type="button" onClick={() => setQuestion(suggestion)}>
            {suggestion}
          </button>
        ))}
      </div>

      <div className="rag-chat-layout">
        <aside className="chat-history">
          <button className="primary-button compact" type="button" onClick={onNewConversation}>
            Chat mới
          </button>
          <div className="chat-history-list">
            {conversations.items.length ? (
              conversations.items.map((conversation) => (
                <button
                  key={conversation.id}
                  className={activeConversationId === conversation.id ? "active" : ""}
                  type="button"
                  onClick={() => onSelectConversation(conversation.id)}
                >
                  <strong>{conversation.title}</strong>
                  <small>{conversation.lastMessagePreview || "Chưa có tin nhắn"}</small>
                </button>
              ))
            ) : (
              <small>Chưa có lịch sử chat.</small>
            )}
          </div>
        </aside>

        <div className="chat-surface">
          <div className="chat-messages">
            {displayMessages.length ? (
              displayMessages.map((message) => (
                <article key={message.id} className={`chat-message ${message.role}`}>
                  <strong>{message.role === "user" ? "Bạn" : "Trợ lý"}</strong>
                  <p>{message.content}</p>
                  {message.role === "assistant" && message.sources?.length ? (
                    <div className="source-list compact">
                      <div className="source-list-title">Nguồn tham khảo</div>
                      {message.sources.slice(0, 3).map((source) => (
                        <article key={source.id ?? source.pointId}>
                          <strong>{source.title || "Tài liệu"}</strong>
                          <span>
                            Điểm: {Number(source.score).toFixed(3)}
                            {source.pageNumber ? ` - Trang ${source.pageNumber}` : ""}
                          </span>
                          {source.sectionTitle ? <small>{source.sectionTitle}</small> : null}
                          {source.content ? (
                            <details>
                              <summary>Xem trích đoạn kiểm chứng</summary>
                              <p>{cleanSourceExcerpt(source.content)}</p>
                            </details>
                          ) : null}
                        </article>
                      ))}
                    </div>
                  ) : null}
                </article>
              ))
            ) : (
              <EmptyState text="Đặt câu hỏi đầu tiên để bắt đầu hội thoại." />
            )}
          </div>

          <form className="rag-chat-form" onSubmit={onSubmit}>
            <div className="chat-input-stack">
              <textarea
                value={question}
                onChange={(event) => setQuestion(event.target.value)}
                placeholder="Ví dụ: Hồ sơ xét tuyển gồm những gì?"
                rows={3}
              />
              <label className="chat-file-input">
                Tệp riêng
                <input
                  type="file"
                  accept=".pdf,.docx,.png,.jpg,.jpeg,.txt,.md"
                  onChange={(event) => setFile(event.target.files?.[0] ?? null)}
                />
                {file ? <span>{file.name}</span> : null}
              </label>
            </div>
            <button className="primary-button" type="submit" disabled={loading}>
              {loading ? "Đang hỏi..." : file ? "Hỏi theo tệp" : "Hỏi tài liệu"}
            </button>
          </form>
          {chat?.error ? <p className="rag-error">{chat.error}</p> : null}
          {chat?.answer ? (
            <div className="feedback-actions" aria-label="Đánh giá câu trả lời">
              <button
                className="ghost-button compact"
                type="button"
                disabled={feedbackLocked}
                onClick={handlePositiveFeedback}
              >
                Hữu ích
              </button>
              <button
                className="ghost-button compact"
                type="button"
                disabled={feedbackLocked}
                onClick={() => setNegativeFeedbackOpen(true)}
              >
                Chưa đúng
              </button>
              {chat.feedbackSubmitted ? <span>Đã lưu: {feedbackLabel(chat.feedbackSubmitted)}</span> : null}
            </div>
          ) : null}
          {chat?.answer && negativeFeedbackOpen && !feedbackLocked ? (
            <div className="feedback-note-box">
              <label htmlFor="negative-feedback-note">Bạn muốn câu trả lời được sửa như thế nào?</label>
              <textarea
                id="negative-feedback-note"
                value={negativeFeedbackNote}
                onChange={(event) => setNegativeFeedbackNote(event.target.value)}
                placeholder="Ví dụ: cần nêu rõ học phí mới nhất, dẫn đúng trang tài liệu hoặc nói rõ điều nào đang sai."
                rows={3}
              />
              <div className="feedback-note-actions">
                <button className="primary-button compact" type="button" onClick={handleNegativeFeedback} disabled={!negativeFeedbackNote.trim()}>
                  Gửi góp ý
                </button>
                <button
                  className="ghost-button compact"
                  type="button"
                  onClick={() => {
                    setNegativeFeedbackOpen(false);
                    setNegativeFeedbackNote("");
                  }}
                >
                  Hủy
                </button>
              </div>
            </div>
          ) : null}
          {chat?.feedbackSubmitted === "negative" && chat?.feedbackNote ? (
            <p className="feedback-note-summary">Góp ý đã gửi: {chat.feedbackNote}</p>
          ) : null}
          {chat?.handoffTicketId ? (
            <p className="handoff-notice">Câu hỏi đã được chuyển cho tư vấn viên. Mã phiếu: {chat.handoffTicketId.slice(0, 8)}</p>
          ) : null}
        </div>
      </div>
    </section>
  );
}

function cleanSourceExcerpt(value) {
  return (value ?? "")
    .replace(/^\s*\.{3,}/, "")
    .replace(/\.{3,}\s*$/, "")
    .replace(/\s+/g, " ")
    .trim();
}

function AdminView({
  token,
  adminUser,
  status,
  login,
  setLogin,
  onLogin,
  forms,
  setForms,
  data,
  users,
  userFilters,
  setUserFilters,
  dashboard,
  aiStatus,
  documents,
  chatFeedbacks,
  chatFeedbackFilter,
  handoffTickets,
  handoffReplies,
  handoffRealtimeStatus,
  setHandoffReplies,
  evaluationQuestions,
  evaluationRuns,
  latestEvaluationRun,
  documentUpload,
  setDocumentUpload,
  onUploadDocument,
  onRefreshUsers,
  onUpdateUserStatus,
  onRefreshDocuments,
  onRefreshChatFeedbacks,
  onChangeChatFeedbackFilter,
  onRefreshHandoffTickets,
  onRefreshDashboard,
  onRefreshAiStatus,
  onRefreshEvaluation,
  onReplyHandoffTicket,
  onUpdateHandoffStatus,
  onSubmit,
}) {
  const [adminTab, setAdminTab] = useState("overview");
  const adminTabs = [
    ["overview", "Tổng quan"],
    ["accounts", "Tài khoản"],
    ["rag", "RAG & hỗ trợ"],
    ["evaluation", "Đánh giá"],
    ["admissions", "Dữ liệu tuyển sinh"],
  ];

  if (!token || !adminUser) {
    return (
      <section className="admin-login">
        <form className="login-form" onSubmit={onLogin}>
          <h2>Đăng nhập quản trị</h2>
          <label>
            Email
            <input value={login.email} onChange={(event) => setLogin({ ...login, email: event.target.value })} />
          </label>
          <label>
            Mật khẩu
            <input
              type="password"
              value={login.password}
              onChange={(event) => setLogin({ ...login, password: event.target.value })}
            />
          </label>
          <button className="primary-button" type="submit">
            Đăng nhập
          </button>
        </form>
      </section>
    );
  }

  return (
    <div className="admin-shell">
      <nav className="admin-tabs" aria-label="Khu vực quản trị">
        {adminTabs.map(([id, label]) => (
          <button key={id} className={adminTab === id ? "active" : ""} type="button" onClick={() => setAdminTab(id)}>
            {label}
          </button>
        ))}
      </nav>

      {adminTab === "overview" ? (
        <div className="admin-grid">
      <AdminDashboardPanel
        token={token}
        dashboard={dashboard}
        aiStatus={aiStatus}
        onRefresh={() => {
          onRefreshDashboard();
          onRefreshAiStatus();
        }}
      />
        </div>
      ) : null}

      {adminTab === "accounts" ? (
        <UserManager
          users={users}
          filters={userFilters}
          setFilters={setUserFilters}
          onRefresh={onRefreshUsers}
          onUpdateStatus={onUpdateUserStatus}
        />
      ) : null}

      {adminTab === "rag" ? (
        <div className="admin-grid">
      <DocumentManager
        documents={documents}
        status={status}
        upload={documentUpload}
        setUpload={setDocumentUpload}
        onUpload={onUploadDocument}
        onRefresh={onRefreshDocuments}
      />

      <ChatFeedbackManager
        feedbacks={chatFeedbacks}
        filter={chatFeedbackFilter}
        onFilterChange={onChangeChatFeedbackFilter}
        onRefresh={onRefreshChatFeedbacks}
      />

      <HandoffTicketManager
        tickets={handoffTickets}
        replies={handoffReplies}
        realtimeStatus={handoffRealtimeStatus}
        setReplies={setHandoffReplies}
        onRefresh={onRefreshHandoffTickets}
        onReply={onReplyHandoffTicket}
          onUpdateStatus={onUpdateHandoffStatus}
      />
        </div>
      ) : null}

      {adminTab === "evaluation" ? (
        <div className="admin-grid">
      <EvaluationManager
        questions={evaluationQuestions}
        runs={evaluationRuns}
        latestRun={latestEvaluationRun}
        status={status}
        onRefresh={onRefreshEvaluation}
      />
        </div>
      ) : null}

      {adminTab === "admissions" ? (
        <div className="admin-grid">
      <AdminForm title="Đợt tuyển sinh" onSubmit={() => onSubmit("cycle", "/api/admin/admissions/cycles", () => forms.cycle)}>
        <Field label="Năm" type="number" value={forms.cycle.year} onChange={(value) => updateForm(setForms, "cycle", "year", Number(value))} />
        <Field label="Tên đợt" value={forms.cycle.name} onChange={(value) => updateForm(setForms, "cycle", "name", value)} />
        <Field label="Ngày bắt đầu" type="date" value={forms.cycle.applicationStartDate} onChange={(value) => updateForm(setForms, "cycle", "applicationStartDate", value)} />
        <Field label="Ngày kết thúc" type="date" value={forms.cycle.applicationEndDate} onChange={(value) => updateForm(setForms, "cycle", "applicationEndDate", value)} />
      </AdminForm>

      <AdminForm title="Khoa" onSubmit={() => onSubmit("faculty", "/api/admin/admissions/faculties", () => forms.faculty)}>
        <Field label="Mã khoa" value={forms.faculty.code} onChange={(value) => updateForm(setForms, "faculty", "code", value)} />
        <Field label="Tên khoa" value={forms.faculty.name} onChange={(value) => updateForm(setForms, "faculty", "name", value)} />
        <Field label="Mô tả" value={forms.faculty.description} onChange={(value) => updateForm(setForms, "faculty", "description", value)} />
      </AdminForm>

      <AdminForm title="Tổ hợp môn" onSubmit={() => onSubmit("subject", "/api/admin/admissions/subject-combinations", () => forms.subject)}>
        <Field label="Mã tổ hợp" value={forms.subject.code} onChange={(value) => updateForm(setForms, "subject", "code", value)} />
        <Field label="Môn thi" value={forms.subject.subjects} onChange={(value) => updateForm(setForms, "subject", "subjects", value)} />
        <Field label="Mô tả" value={forms.subject.description} onChange={(value) => updateForm(setForms, "subject", "description", value)} />
      </AdminForm>

      <AdminForm title="Phương thức" onSubmit={() => onSubmit("method", "/api/admin/admissions/methods", () => forms.method)}>
        <Field label="Mã phương thức" value={forms.method.code} onChange={(value) => updateForm(setForms, "method", "code", value)} />
        <Field label="Tên phương thức" value={forms.method.name} onChange={(value) => updateForm(setForms, "method", "name", value)} />
        <Field label="Mô tả" value={forms.method.description} onChange={(value) => updateForm(setForms, "method", "description", value)} />
      </AdminForm>

      <AdminForm title="Ngành" onSubmit={() => onSubmit("major", "/api/admin/admissions/majors", () => forms.major)}>
        <SelectField label="Khoa" value={forms.major.facultyId} onChange={(value) => updateForm(setForms, "major", "facultyId", value)} options={data.faculties} />
        <Field label="Mã ngành" value={forms.major.code} onChange={(value) => updateForm(setForms, "major", "code", value)} />
        <Field label="Tên ngành" value={forms.major.name} onChange={(value) => updateForm(setForms, "major", "name", value)} />
        <Field label="Mô tả" value={forms.major.description} onChange={(value) => updateForm(setForms, "major", "description", value)} />
        <Field label="Nghề nghiệp" value={forms.major.careerOutcomes} onChange={(value) => updateForm(setForms, "major", "careerOutcomes", value)} />
      </AdminForm>

      <AdminForm title="Chương trình" onSubmit={() => onSubmit("program", "/api/admin/admissions/programs", () => forms.program)}>
        <SelectField label="Ngành" value={forms.program.majorId} onChange={(value) => updateForm(setForms, "program", "majorId", value)} options={data.majors.items} />
        <Field label="Mã chương trình" value={forms.program.code} onChange={(value) => updateForm(setForms, "program", "code", value)} />
        <Field label="Tên chương trình" value={forms.program.name} onChange={(value) => updateForm(setForms, "program", "name", value)} />
        <Field label="Cơ sở" value={forms.program.campus} onChange={(value) => updateForm(setForms, "program", "campus", value)} />
        <label>
          Tổ hợp môn
          <select
            multiple
            value={forms.program.subjectCombinationIds}
            onChange={(event) =>
              updateForm(
                setForms,
                "program",
                "subjectCombinationIds",
                Array.from(event.target.selectedOptions, (option) => option.value),
              )
            }
          >
            {data.subjects.map((subject) => (
              <option key={subject.id} value={subject.id}>
                {subject.code} - {subject.subjects}
              </option>
            ))}
          </select>
        </label>
      </AdminForm>

      <AdminForm title="Điểm chuẩn" onSubmit={() => onSubmit("cutoff", "/api/admin/admissions/cutoff-scores", () => forms.cutoff)}>
        <SelectField label="Chương trình" value={forms.cutoff.programId} onChange={(value) => updateForm(setForms, "cutoff", "programId", value)} options={programOptions(data)} />
        <SelectField label="Đợt" value={forms.cutoff.admissionCycleId} onChange={(value) => updateForm(setForms, "cutoff", "admissionCycleId", value)} options={data.cycles} />
        <SelectField label="Phương thức" value={forms.cutoff.admissionMethodId} onChange={(value) => updateForm(setForms, "cutoff", "admissionMethodId", value)} options={data.methods} />
        <SelectField label="Tổ hợp" value={forms.cutoff.subjectCombinationId} onChange={(value) => updateForm(setForms, "cutoff", "subjectCombinationId", value)} options={data.subjects} />
        <Field label="Điểm" type="number" value={forms.cutoff.score} onChange={(value) => updateForm(setForms, "cutoff", "score", Number(value))} />
      </AdminForm>

      <AdminForm title="Học phí" onSubmit={() => onSubmit("tuition", "/api/admin/admissions/tuition-fees", () => forms.tuition)}>
        <SelectField label="Chương trình" value={forms.tuition.programId} onChange={(value) => updateForm(setForms, "tuition", "programId", value)} options={programOptions(data)} />
        <Field label="Năm học" value={forms.tuition.academicYear} onChange={(value) => updateForm(setForms, "tuition", "academicYear", value)} />
        <Field label="Từ" type="number" value={forms.tuition.amountMin} onChange={(value) => updateForm(setForms, "tuition", "amountMin", Number(value))} />
        <Field label="Đến" type="number" value={forms.tuition.amountMax} onChange={(value) => updateForm(setForms, "tuition", "amountMax", Number(value))} />
      </AdminForm>

      <AdminForm title="FAQ" onSubmit={() => onSubmit("faq", "/api/admin/admissions/faqs", () => forms.faq)}>
        <Field label="Danh mục" value={forms.faq.category} onChange={(value) => updateForm(setForms, "faq", "category", value)} />
        <Field label="Câu hỏi" value={forms.faq.question} onChange={(value) => updateForm(setForms, "faq", "question", value)} />
        <Field label="Trả lời" value={forms.faq.answer} onChange={(value) => updateForm(setForms, "faq", "answer", value)} />
      </AdminForm>
        </div>
      ) : null}
    </div>
  );
}

function UserManager({ users, filters, setFilters, onRefresh, onUpdateStatus }) {
  function updateFilter(key, value) {
    const next = { ...filters, [key]: value };
    setFilters(next);
    onRefresh(next, 1);
  }

  return (
    <section className="admin-card user-manager">
      <div className="section-title">
        <div>
          <h2>Quản lý tài khoản</h2>
          <p>{users.totalItems ?? users.items.length} tài khoản phù hợp bộ lọc.</p>
        </div>
        <button className="ghost-button" type="button" onClick={() => onRefresh(filters, users.page || 1)}>
          Tải lại
        </button>
      </div>

      <div className="user-filters">
        <Field label="Tìm theo tên/email" value={filters.keyword} onChange={(value) => updateFilter("keyword", value)} />
        <label>
          Vai trò
          <select value={filters.role} onChange={(event) => updateFilter("role", event.target.value)}>
            <option value="">Tất cả</option>
            <option value="student">Sinh viên</option>
            <option value="parent">Phụ huynh</option>
            <option value="staff">Nhân viên</option>
            <option value="admin">Quản trị viên</option>
          </select>
        </label>
        <label>
          Trạng thái
          <select value={filters.status} onChange={(event) => updateFilter("status", event.target.value)}>
            <option value="">Tất cả</option>
            <option value="active">Đang hoạt động</option>
            <option value="inactive">Đã khóa</option>
          </select>
        </label>
      </div>

      <div className="user-table">
        <table>
          <thead>
            <tr>
              <th>Người dùng</th>
              <th>Vai trò</th>
              <th>Trạng thái</th>
              <th>Ngày tạo</th>
              <th>Lệnh</th>
            </tr>
          </thead>
          <tbody>
            {users.items.length ? (
              users.items.map((user) => (
                <tr key={user.id}>
                  <td>
                    <strong>{user.fullName}</strong>
                    <small>{user.email}</small>
                  </td>
                  <td>{user.roles.map(accountRoleLabel).join(", ")}</td>
                  <td>{statusLabel(user.status)}</td>
                  <td>{new Date(user.createdAt).toLocaleDateString("vi-VN")}</td>
                  <td>
                    <div className="row-actions">
                      {user.status === "active" ? (
                        <button className="ghost-button" type="button" onClick={() => onUpdateStatus(user.id, "inactive", filters, users.page || 1)}>
                          Khóa
                        </button>
                      ) : (
                        <button className="primary-button compact" type="button" onClick={() => onUpdateStatus(user.id, "active", filters, users.page || 1)}>
                          Mở khóa
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan="5">
                  <EmptyState text="Chưa có tài khoản phù hợp bộ lọc." />
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="pagination-row">
        <button className="ghost-button" type="button" disabled={(users.page ?? 1) <= 1} onClick={() => onRefresh(filters, (users.page ?? 1) - 1)}>
          Trước
        </button>
        <span>
          Trang {users.page ?? 1}/{users.totalPages ?? 1}
        </span>
        <button
          className="ghost-button"
          type="button"
          disabled={(users.page ?? 1) >= (users.totalPages ?? 1)}
          onClick={() => onRefresh(filters, (users.page ?? 1) + 1)}
        >
          Sau
        </button>
      </div>
    </section>
  );
}

function DocumentManager({ documents, status, upload, setUpload, onUpload, onRefresh }) {
  const statusMessage = status?.message ?? "";
  const relevantStatus = Boolean(status?.error || /tài liệu|đoạn|RAG/i.test(statusMessage));
  const busy = Boolean(status?.loading && relevantStatus);

  return (
    <section className="admin-card document-manager">
      <div className="section-title">
        <div>
          <h2>Tài liệu RAG</h2>
          <p>Nạp PDF/DOCX/ảnh/TXT, tách thành đoạn nhỏ, tạo embedding và đưa vào Qdrant để chatbot tra cứu.</p>
        </div>
        <button className="ghost-button" type="button" onClick={onRefresh} disabled={busy}>
          Tải lại
        </button>
      </div>
      <InlineStatus status={relevantStatus ? status : null} />
      <form className="document-upload" onSubmit={onUpload}>
        <Field label="Tiêu đề" value={upload.title} onChange={(value) => setUpload({ ...upload, title: value })} />
        <label>
          Loại tài liệu
          <select value={upload.documentType} onChange={(event) => setUpload({ ...upload, documentType: event.target.value })}>
            <option value="regulation">Quy chế</option>
            <option value="tuition">Học phí</option>
            <option value="policy">Chính sách</option>
            <option value="faq">FAQ</option>
            <option value="admission_notice">Thông báo tuyển sinh</option>
          </select>
        </label>
        <Field label="Nguồn" value={upload.source} onChange={(value) => setUpload({ ...upload, source: value })} />
        <label>
          Tệp PDF/DOCX/ảnh/TXT
          <input
            type="file"
            accept=".pdf,.docx,.png,.jpg,.jpeg,.txt,.md"
            onChange={(event) => setUpload({ ...upload, file: event.target.files?.[0] ?? null })}
          />
        </label>
        <button className="primary-button compact" type="submit" disabled={busy}>
          {busy ? "Đang xử lý..." : "Tải lên"}
        </button>
      </form>

      <div className="document-table">
        <table>
          <thead>
            <tr>
              <th>Tài liệu</th>
              <th>Loại</th>
              <th>Trạng thái</th>
              <th>Đoạn</th>
            </tr>
          </thead>
          <tbody>
            {documents.items.map((document) => {
              const version = document.versions[0];
              return (
                <tr key={document.id}>
                  <td>
                    <strong>{document.title}</strong>
                    <small>{version?.fileName}</small>
                  </td>
                  <td>{documentTypeLabel(document.documentType)}</td>
                  <td>{statusLabel(version?.processingStatus || document.status)}</td>
                  <td>{version?.chunkCount ?? 0}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

    </section>
  );
}

function ChatFeedbackManager({ feedbacks, filter, onFilterChange, onRefresh }) {
  return (
    <section className="admin-card feedback-manager">
      <div className="section-title">
        <div>
          <h2>Đánh giá chat RAG</h2>
          <p>{feedbacks.totalItems ?? feedbacks.items.length} đánh giá gần đây</p>
        </div>
        <div className="feedback-toolbar">
          <label className="feedback-filter">
            <span>Lọc đánh giá</span>
            <select value={filter} onChange={(event) => onFilterChange(event.target.value)}>
              <option value="all">Tất cả</option>
              <option value="positive">Hữu ích</option>
              <option value="negative">Chưa đúng</option>
            </select>
          </label>
          <button className="ghost-button" type="button" onClick={onRefresh}>
            Tải lại
          </button>
        </div>
      </div>
      <div className="feedback-list">
        {feedbacks.items.length ? (
          feedbacks.items.map((item) => (
            <article key={item.id}>
              <div className="feedback-head">
                <span className={`rating-pill ${item.rating}`}>{feedbackLabel(item.rating)}</span>
                <small>{new Date(item.createdAt).toLocaleString("vi-VN")}</small>
              </div>
              <strong>{item.question || "Không có câu hỏi"}</strong>
              <p>{item.answer}</p>
              <div className="feedback-meta">
                <small>{item.userEmail || "Khách vãng lai"}</small>
                {item.handoffTicketId ? <small>Ticket #{item.handoffTicketId.slice(0, 8)}</small> : null}
              </div>
              {item.note ? <small className="feedback-note">{item.note}</small> : null}
            </article>
          ))
        ) : (
          <EmptyState text="Chưa có đánh giá chat nào." />
        )}
      </div>
    </section>
  );
}

function HandoffTicketManager({ tickets, replies, realtimeStatus, setReplies, onRefresh, onReply, onUpdateStatus }) {
  return (
    <section className="admin-card handoff-manager">
      <div className="section-title">
        <div>
          <h2>Phiếu hỗ trợ trực tiếp</h2>
          <p>
            {tickets.totalItems ?? tickets.items.length} phiếu cần tư vấn viên xử lý
            <span className={`realtime-status ${realtimeStatus}`}>Thời gian thực: {realtimeStatusLabel(realtimeStatus)}</span>
          </p>
        </div>
        <button className="ghost-button" type="button" onClick={onRefresh}>
          Tải lại
        </button>
      </div>
      <div className="handoff-list">
        {tickets.items.length ? (
          tickets.items.map((ticket) => (
            <article key={ticket.id} className={ticket.status}>
              <div className="handoff-head">
                <div>
                  <span className={`status-pill ${ticket.status}`}>{statusLabel(ticket.status)}</span>
                  <span className={`priority-pill ${ticket.priority}`}>{priorityLabel(ticket.priority)}</span>
                </div>
                <small>{new Date(ticket.updatedAt).toLocaleString("vi-VN")}</small>
              </div>
              <strong>{ticket.question || "Không có câu hỏi"}</strong>
              <p>{ticket.aiAnswer}</p>
              {ticket.messages?.length ? (
                <div className="handoff-messages">
                  {ticket.messages.map((message) => (
                    <p key={message.id}>
                      <strong>{roleLabel(message.senderRole)}:</strong> {message.content}
                    </p>
                  ))}
                </div>
              ) : null}
              <textarea
                rows={2}
                value={replies[ticket.id] ?? ""}
                onChange={(event) => setReplies((current) => ({ ...current, [ticket.id]: event.target.value }))}
                placeholder="Nhập phản hồi của tư vấn viên"
              />
              <div className="row-actions">
                <button className="ghost-button" type="button" onClick={() => onReply(ticket.id, false)}>
                  Gửi phản hồi
                </button>
                <button className="primary-button compact" type="button" onClick={() => onReply(ticket.id, true)}>
                  Phản hồi và đóng
                </button>
                {ticket.status !== "closed" ? (
                  <button className="ghost-button" type="button" onClick={() => onUpdateStatus(ticket.id, "closed")}>
                    Đóng phiếu
                  </button>
                ) : null}
              </div>
            </article>
          ))
        ) : (
          <EmptyState text="Chưa có phiếu hỗ trợ trực tiếp nào." />
        )}
      </div>
    </section>
  );
}

function AdminDashboardPanel({ token, dashboard, aiStatus, onRefresh }) {
  const [runtimeStatus, setRuntimeStatus] = useState(null);
  const visibleAiStatus = normalizeAiStatus(runtimeStatus) ?? normalizeAiStatus(aiStatus);

  useEffect(() => {
    if (aiStatus) {
      setRuntimeStatus(aiStatus);
    }
  }, [aiStatus]);

  useEffect(() => {
    if (!token) return undefined;
    let disposed = false;
    fetchAiStatus(token)
      .then((result) => {
        if (!disposed) setRuntimeStatus(result);
      })
      .catch(() => {
        if (!disposed) setRuntimeStatus(null);
      });
    return () => {
      disposed = true;
    };
  }, [token]);

  async function refreshPanel() {
    onRefresh();
    if (!token) return;
    try {
      const result = await fetchAiStatus(token);
      setRuntimeStatus(result);
    } catch {
      setRuntimeStatus(null);
    }
  }

  return (
    <section className="admin-card dashboard-panel">
      <div className="section-title">
        <div>
          <h2>Bảng điều khiển vận hành</h2>
          <p>Tổng quan chat, tài liệu, chuyển tư vấn viên và đánh giá.</p>
        </div>
        <button className="ghost-button" type="button" onClick={refreshPanel}>
          Tải lại
        </button>
      </div>
      {dashboard ? (
        <div className="dashboard-metrics">
          <Metric label="Người dùng" value={dashboard.totalUsers} />
          <Metric label="Tài liệu" value={dashboard.totalDocuments} />
          <Metric label="Tài liệu xong" value={dashboard.completedDocumentVersions} />
          <Metric label="Cuộc chat" value={dashboard.totalConversations} />
          <Metric label="Tin nhắn" value={dashboard.totalChatMessages} />
          <Metric label="Đánh giá âm" value={dashboard.negativeFeedback} />
          <Metric label="Phiếu mở" value={dashboard.openHandoffTickets} />
          <Metric label="Đã xử lý" value={dashboard.resolvedHandoffTickets} />
          <Metric label="Lượt đánh giá" value={dashboard.evaluationRuns} />
          <Metric label="Hit@K" value={percent(dashboard.latestEvaluationHitRateAtK)} />
          <Metric label="Từ khóa" value={percent(dashboard.latestEvaluationKeywordHitRate)} />
          <Metric label="Độ trễ" value={`${Math.round(dashboard.averageChatLatencyMs)}ms`} />
          <Metric label="Dịch vụ AI" value={statusLabel(visibleAiStatus?.aiServiceStatus)} />
          <Metric label="Vector" value={visibleAiStatus?.vectorBackend ?? "-"} />
          <Metric label="Qdrant" value={visibleAiStatus?.qdrantAvailable ? "bật" : "tắt"} />
          <Metric label="LLM" value={visibleAiStatus?.llmConfigured ? "sẵn sàng" : "tắt"} />
        </div>
      ) : (
        <EmptyState text="Đang chờ dữ liệu bảng điều khiển." />
      )}
    </section>
  );
}

async function fetchAiStatus(token) {
  const response = await fetch(`${API_BASE}/api/admin/ai/status`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok || payload?.success === false) {
    throw new Error(payload?.message || response.statusText);
  }
  return payload?.data ?? payload;
}

function normalizeAiStatus(value) {
  if (!value) return null;
  const candidate = value.data ?? value;
  if (!candidate || typeof candidate !== "object") return null;
  return candidate.aiServiceStatus || candidate.vectorBackend || "qdrantAvailable" in candidate || "llmConfigured" in candidate
    ? candidate
    : null;
}

function EvaluationManager({ questions, runs, latestRun, status, onRefresh }) {
  const statusMessage = status?.message ?? "";
  const relevantStatus = Boolean(status?.error || /đánh giá|câu hỏi chuẩn/i.test(statusMessage));
  const busy = Boolean(status?.loading && relevantStatus);
  const hasResults = Boolean(latestRun?.results?.length);

  return (
    <section className="admin-card evaluation-manager">
      <div className="section-title">
        <div>
          <h2>Đánh giá RAG</h2>
          <p>{questions.length} câu hỏi chuẩn - {runs.totalItems ?? runs.items.length} lần chạy. Hit@K đo việc RAG có lấy trúng nguồn trong Top K đoạn hay không.</p>
        </div>
        <div className="row-actions">
          <button className="ghost-button" type="button" onClick={onRefresh} disabled={busy}>
            Tải lại
          </button>
        </div>
      </div>
      <InlineStatus status={relevantStatus ? status : null} />

      {latestRun && hasResults ? (
        <>
          <div className="run-summary">
            <strong>{latestRun.name}</strong>
            <span>{evaluationStatusLabel(latestRun.status)} - TopK {latestRun.topK}</span>
          </div>
          <div className="evaluation-metrics">
            <Metric label="Đúng" value={`${latestRun.correctQuestions}/${latestRun.totalQuestions}`} />
            <Metric label="Hit@K" value={percent(latestRun.hitRateAtK)} />
            <Metric label="Từ khóa" value={percent(latestRun.averageKeywordHitRate)} />
            <Metric label="Độ trễ TB" value={`${Math.round(latestRun.averageLatencyMs)}ms`} />
          </div>
          <div className="evaluation-results">
            {latestRun.results.map((result) => (
              <article key={result.id} className={result.isCorrect ? "correct" : "incorrect"}>
                <div>
                  <strong>{result.questionCode}</strong>
                  <span>{result.isCorrect ? "Đúng" : "Cần xem lại"}</span>
                </div>
                <p>{result.question}</p>
                <small>
                  Điểm cao nhất {Number(result.topScore).toFixed(3)} - Hit@K {result.hitAtK ? "có" : "không"} - Từ khóa {percent(result.keywordHitRate)} - {result.latencyMs}ms
                </small>
                <p>{result.answerPreview || result.errorMessage || "Không có kết quả."}</p>
              </article>
            ))}
          </div>
        </>
      ) : latestRun ? (
        <EmptyState text={`Lần chạy gần nhất đang ở trạng thái ${evaluationStatusLabel(latestRun.status)} và chưa có kết quả chi tiết. Hệ thống sẽ tự động cập nhật sau khi xử lý tài liệu.`} />
      ) : (
        <EmptyState text="Chưa có lần chạy đánh giá nào." />
      )}
    </section>
  );
}

function InlineStatus({ status }) {
  if (!status?.loading && !status?.message && !status?.error) return null;
  return (
    <div className={`inline-status ${status?.error ? "error" : status?.loading ? "loading" : "ok"}`}>
      {status?.error || status?.message || "Đang xử lý..."}
    </div>
  );
}

function AdminForm({ title, children, onSubmit }) {
  return (
    <form
      className="admin-card"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit();
      }}
    >
      <h2>{title}</h2>
      <div className="form-grid">{children}</div>
      <button className="primary-button compact" type="submit">
        Lưu
      </button>
    </form>
  );
}

function Field({ label, value, onChange, type = "text", disabled = false }) {
  return (
    <label>
      {label}
      <input type={type} value={value ?? ""} disabled={disabled} onChange={(event) => onChange?.(event.target.value)} />
    </label>
  );
}

function SelectField({ label, value, onChange, options }) {
  return (
    <label>
      {label}
      <select value={value ?? ""} onChange={(event) => onChange(event.target.value)}>
        <option value="">Chọn</option>
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.name || option.code}
          </option>
        ))}
      </select>
    </label>
  );
}

function Metric({ label, value }) {
  return (
    <div className="metric">
      <strong>{value}</strong>
      <span>{label}</span>
    </div>
  );
}

function EmptyState({ text }) {
  return <div className="empty-state">{text}</div>;
}

function initialAdminForms() {
  const year = todayYear() + 1;
  return {
    cycle: {
      year,
      name: `Tuyển sinh ${year}`,
      applicationStartDate: `${year}-03-01`,
      applicationEndDate: `${year}-08-31`,
      status: "active",
    },
    faculty: { code: "", name: "", description: "", status: "active" },
    subject: { code: "", subjects: "", description: "" },
    method: { code: "", name: "", description: "", status: "active" },
    major: { ...emptyMajor },
    program: { ...emptyProgram },
    cutoff: {
      programId: "",
      admissionCycleId: "",
      admissionMethodId: "",
      subjectCombinationId: "",
      score: 20,
      note: "",
    },
    tuition: {
      programId: "",
      academicYear: `${year}-${year + 1}`,
      amountMin: 0,
      amountMax: 0,
      currency: "VND",
      unit: "year",
      note: "",
    },
    faq: { category: "", question: "", answer: "", status: "active" },
  };
}

function updateForm(setForms, formName, fieldName, value) {
  setForms((current) => ({
    ...current,
    [formName]: {
      ...current[formName],
      [fieldName]: value,
    },
  }));
}

function resetAdminForm(current, kind) {
  const fresh = initialAdminForms();
  return {
    ...current,
    [kind]: fresh[kind],
  };
}

function programOptions(data) {
  return data.majors.items.flatMap((major) =>
    major.programs.map((program) => ({
      id: program.id,
      name: `${program.name} - ${major.name}`,
    })),
  );
}

function statusLabel(value) {
  const labels = {
    active: "Đang hoạt động",
    inactive: "Ngừng hoạt động",
    processing: "Đang xử lý",
    pending: "Đang chờ",
    completed: "Hoàn tất",
    failed: "Lỗi",
    open: "Đang mở",
    in_progress: "Đang xử lý",
    resolved: "Đã xử lý",
    closed: "Đã đóng",
    ok: "Ổn định",
  };
  return labels[value] ?? value ?? "-";
}

function documentTypeLabel(value) {
  const labels = {
    regulation: "Quy chế",
    tuition: "Học phí",
    policy: "Chính sách",
    faq: "FAQ",
    admission_notice: "Thông báo tuyển sinh",
    chat_upload: "Tệp chat",
  };
  return labels[value] ?? value ?? "-";
}

function feedbackLabel(value) {
  const labels = {
    positive: "Hữu ích",
    negative: "Chưa đúng",
  };
  return labels[value] ?? value ?? "-";
}

function priorityLabel(value) {
  const labels = {
    low: "Thấp",
    normal: "Bình thường",
    high: "Cao",
    urgent: "Khẩn cấp",
  };
  return labels[value] ?? value ?? "-";
}

function roleLabel(value) {
  const labels = {
    user: "Người dùng",
    assistant: "Trợ lý",
    staff: "Tư vấn viên",
    admin: "Quản trị viên",
    guest: "Khách",
  };
  return labels[value] ?? value ?? "-";
}

function accountRoleLabel(value) {
  const labels = {
    student: "Sinh viên",
    parent: "Phụ huynh",
    staff: "Nhân viên trường",
    admin: "Quản trị viên",
  };
  return labels[value] ?? value ?? "-";
}

function realtimeStatusLabel(value) {
  const labels = {
    online: "trực tuyến",
    offline: "ngoại tuyến",
    reconnecting: "đang kết nối lại",
    limited: "giới hạn",
  };
  return labels[value] ?? value ?? "-";
}

function evaluationStatusLabel(value) {
  const labels = {
    running: "Đang chạy",
    completed: "Hoàn tất",
    completed_with_errors: "Hoàn tất nhưng có lỗi",
    failed: "Lỗi",
  };
  return labels[value] ?? value ?? "-";
}

function money(value) {
  if (value === null || value === undefined) return "-";
  return Number(value).toLocaleString("vi-VN");
}

function percent(value) {
  if (value === null || value === undefined) return "-";
  return `${Math.round(Number(value) * 100)}%`;
}

export default App;
