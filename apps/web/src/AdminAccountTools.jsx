import { useEffect, useMemo, useState } from "react";
import { createPortal } from "react-dom";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

const roleOptions = [
  ["student", "Sinh viên"],
  ["parent", "Phụ huynh"],
  ["staff", "Nhân viên"],
  ["admin", "Quản trị viên"],
];

async function adminApi(path, token, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      ...(options.headers ?? {}),
    },
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok || payload?.success === false) {
    throw new Error(payload?.message || payload?.error?.message || response.statusText);
  }
  return payload?.data ?? payload;
}

export default function AdminAccountTools() {
  const [target, setTarget] = useState(null);
  const [users, setUsers] = useState([]);
  const [selectedUserId, setSelectedUserId] = useState("");
  const [selectedRole, setSelectedRole] = useState("student");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    fullName: "",
    email: "",
    phone: "",
    role: "student",
    temporaryPassword: "",
  });

  useEffect(() => {
    const syncTarget = () => setTarget(document.querySelector(".user-manager"));
    syncTarget();
    const observer = new MutationObserver(syncTarget);
    observer.observe(document.body, { childList: true, subtree: true });
    return () => observer.disconnect();
  }, []);

  const token = target ? localStorage.getItem("admissions_token") ?? "" : "";

  async function loadUsers() {
    if (!token) return;
    try {
      const result = await adminApi("/api/admin/users?page=1&pageSize=100", token);
      setUsers(result.items ?? []);
      if (!selectedUserId && result.items?.length) {
        setSelectedUserId(result.items[0].id);
        setSelectedRole(result.items[0].roles?.[0] ?? "student");
      }
    } catch (loadError) {
      setError(loadError.message);
    }
  }

  useEffect(() => {
    if (target && token) loadUsers();
  }, [target, token]);

  const selectedUser = useMemo(
    () => users.find((user) => user.id === selectedUserId),
    [users, selectedUserId],
  );

  function notifyParentRefresh() {
    const button = [...document.querySelectorAll(".user-manager .section-title button")]
      .find((item) => item.textContent?.trim() === "Tải lại");
    button?.click();
  }

  async function createAccount(event) {
    event.preventDefault();
    if (!token) return;
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const created = await adminApi("/api/admin/users", token, {
        method: "POST",
        body: JSON.stringify(form),
      });
      setMessage(`Đã tạo ${created.fullName} (${created.email}) với vai trò ${labelRole(form.role)}.`);
      setForm({ fullName: "", email: "", phone: "", role: "student", temporaryPassword: "" });
      await loadUsers();
      notifyParentRefresh();
    } catch (createError) {
      setError(createError.message);
    } finally {
      setBusy(false);
    }
  }

  async function changeRole(event) {
    event.preventDefault();
    if (!token || !selectedUserId) return;
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const updated = await adminApi(`/api/admin/users/${selectedUserId}/roles`, token, {
        method: "PUT",
        body: JSON.stringify({ roles: [selectedRole] }),
      });
      setMessage(`Đã đổi ${updated.fullName} sang vai trò ${labelRole(selectedRole)}.`);
      await loadUsers();
      notifyParentRefresh();
    } catch (roleError) {
      setError(roleError.message);
    } finally {
      setBusy(false);
    }
  }

  if (!target || !token) return null;

  return createPortal(
    <div className="managed-account-tools" aria-label="Tạo tài khoản và phân quyền">
      <div className="managed-account-head">
        <div>
          <span>QUẢN TRỊ TÀI KHOẢN</span>
          <h3>Tạo tài khoản & đổi vai trò</h3>
          <p>Tạo trực tiếp tài khoản sinh viên, phụ huynh, nhân viên hoặc quản trị viên; có thể đổi vai trò sau đó.</p>
        </div>
        <button className="ghost-button compact" type="button" onClick={loadUsers} disabled={busy}>Làm mới</button>
      </div>

      <div className="managed-account-grid">
        <form onSubmit={createAccount}>
          <h4>Tạo tài khoản mới</h4>
          <label>Họ tên<input required value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} /></label>
          <label>Email<input required type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
          <label>Số điện thoại<input value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} /></label>
          <label>Loại tài khoản<select value={form.role} onChange={(event) => setForm({ ...form, role: event.target.value })}>{roleOptions.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
          <label>Mật khẩu tạm<input required minLength={8} type="password" value={form.temporaryPassword} onChange={(event) => setForm({ ...form, temporaryPassword: event.target.value })} /></label>
          <button className="primary-button compact" type="submit" disabled={busy}>{busy ? "Đang lưu..." : "Tạo tài khoản"}</button>
        </form>

        <form onSubmit={changeRole}>
          <h4>Đổi vai trò tài khoản</h4>
          <label>Tài khoản cần đổi<select value={selectedUserId} onChange={(event) => {
            const id = event.target.value;
            setSelectedUserId(id);
            const user = users.find((item) => item.id === id);
            setSelectedRole(user?.roles?.[0] ?? "student");
          }}>{users.map((user) => <option key={user.id} value={user.id}>{user.fullName} — {user.email}</option>)}</select></label>
          <label>Quyền mới<select value={selectedRole} onChange={(event) => setSelectedRole(event.target.value)}>{roleOptions.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
          {selectedUser ? <p className="managed-current-role">Hiện tại: {selectedUser.roles?.map(labelRole).join(", ") || "Chưa có vai trò"}</p> : null}
          <p className="managed-account-note">Hệ thống không cho tự hạ quyền tài khoản đang đăng nhập và không cho hạ quản trị viên cuối cùng.</p>
          <button className="primary-button compact" type="submit" disabled={busy || !selectedUserId}>{busy ? "Đang lưu..." : "Đổi vai trò"}</button>
        </form>
      </div>
      {message ? <p className="managed-account-message ok">{message}</p> : null}
      {error ? <p className="managed-account-message error">{error}</p> : null}
    </div>,
    target,
  );
}

function labelRole(role) {
  return Object.fromEntries(roleOptions)[role] ?? role;
}