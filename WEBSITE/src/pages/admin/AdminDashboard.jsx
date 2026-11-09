import { useCallback, useEffect, useMemo, useState } from "react";
import { getAuth } from "firebase/auth";
import { useNavigate } from "react-router-dom";
import {
  Activity,
  ArrowLeft,
  Check,
  ChevronDown,
  Database,
  FilePlus2,
  FolderOpen,
  GraduationCap,
  LoaderCircle,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldCheck,
  Trash2,
  UserPlus,
  Users,
  X,
} from "lucide-react";

const API_BASE = import.meta.env.VITE_API_URL || "http://localhost:5000";
const card = "rounded-2xl border border-slate-200 bg-white shadow-sm";
const input =
  "w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100";
const buttonPrimary =
  "inline-flex items-center justify-center gap-2 rounded-xl bg-indigo-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-800 disabled:cursor-not-allowed disabled:opacity-50";
const buttonSecondary =
  "inline-flex items-center justify-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:opacity-50";

function getMessage(data, fallback) {
  return data?.error || data?.message || fallback;
}

export default function AdminDashboard() {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState("users");
  const [users, setUsers] = useState([]);
  const [userSearch, setUserSearch] = useState("");
  const [collections, setCollections] = useState([]);
  const [selectedCollection, setSelectedCollection] = useState("");
  const [documents, setDocuments] = useState([]);
  const [selectedDocument, setSelectedDocument] = useState(null);
  const [documentJson, setDocumentJson] = useState("{}");
  const [documentId, setDocumentId] = useState("");
  const [isNewDocument, setIsNewDocument] = useState(false);
  const [showInstructorForm, setShowInstructorForm] = useState(false);
  const [instructor, setInstructor] = useState({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
  });
  const [busy, setBusy] = useState(false);
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [loadingDatabase, setLoadingDatabase] = useState(false);
  const [notice, setNotice] = useState({ type: "", text: "" });
  const [error, setError] = useState("");

  const apiFetch = useCallback(async (path, options = {}) => {
    const currentUser = getAuth().currentUser;
    if (!currentUser)
      throw new Error("Your session has expired. Please sign in again.");
    const token = await currentUser.getIdToken();
    const response = await fetch(`${API_BASE}${path}`, {
      ...options,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        ...(options.headers || {}),
      },
    });
    const data =
      response.status === 204 ? {} : await response.json().catch(() => ({}));
    if (!response.ok)
      throw new Error(getMessage(data, "The request could not be completed."));
    return data;
  }, []);

  const loadUsers = useCallback(async () => {
    setLoadingUsers(true);
    try {
      const data = await apiFetch("/api/users");
      setUsers(Array.isArray(data) ? data : []);
      setError("");
    } catch (err) {
      setError(err.message);
    } finally {
      setLoadingUsers(false);
    }
  }, [apiFetch]);

  const loadCollections = useCallback(
    async (preferredCollection) => {
      setLoadingDatabase(true);
      try {
        const data = await apiFetch("/api/admin/collections");
        const items = Array.isArray(data) ? data : [];
        setCollections(items);
        const nextCollection =
          preferredCollection || selectedCollection || items[0]?.id || "";
        setSelectedCollection(nextCollection);
        if (nextCollection) await loadDocuments(nextCollection);
        else {
          setDocuments([]);
          setSelectedDocument(null);
        }
      } catch (err) {
        setNotice({ type: "error", text: err.message });
      } finally {
        setLoadingDatabase(false);
      }
    },
    [apiFetch, selectedCollection],
  );

  const loadDocuments = useCallback(
    async (collectionId = selectedCollection) => {
      if (!collectionId) return;
      setLoadingDatabase(true);
      try {
        const data = await apiFetch(
          `/api/admin/collections/${encodeURIComponent(collectionId)}/documents`,
        );
        setDocuments(Array.isArray(data) ? data : []);
        setSelectedDocument(null);
        setDocumentJson("{}");
        setNotice({ type: "", text: "" });
      } catch (err) {
        setNotice({ type: "error", text: err.message });
      } finally {
        setLoadingDatabase(false);
      }
    },
    [apiFetch, selectedCollection],
  );

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);
  useEffect(() => {
    if (activeTab === "database" && collections.length === 0) loadCollections();
  }, [activeTab, collections.length, loadCollections]);

  const filteredUsers = useMemo(() => {
    const query = userSearch.trim().toLowerCase();
    if (!query) return users;
    return users.filter((user) =>
      [
        user.name,
        user.firstName,
        user.lastName,
        user.email,
        user.role,
        user.studentId,
        user.uid,
      ].some((value) =>
        String(value || "")
          .toLowerCase()
          .includes(query),
      ),
    );
  }, [users, userSearch]);

  const submitInstructor = async (event) => {
    event.preventDefault();
    setBusy(true);
    setNotice({ type: "", text: "" });
    try {
      const fullName =
        `${instructor.firstName.trim()} ${instructor.lastName.trim()}`.trim();
      await apiFetch("/api/users", {
        method: "POST",
        body: JSON.stringify({
          firstName: instructor.firstName.trim(),
          lastName: instructor.lastName.trim(),
          name: fullName,
          email: instructor.email.trim().toLowerCase(),
          password: instructor.password,
          role: "instructor",
          status: "Active",
        }),
      });
      setInstructor({ firstName: "", lastName: "", email: "", password: "" });
      setShowInstructorForm(false);
      setNotice({
        type: "success",
        text: "Instructor account created successfully.",
      });
      await loadUsers();
    } catch (err) {
      setNotice({ type: "error", text: err.message });
    } finally {
      setBusy(false);
    }
  };

  const openDocument = (doc) => {
    setSelectedDocument(doc);
    setDocumentId(doc.id);
    setDocumentJson(JSON.stringify(doc.data ?? {}, null, 2));
    setIsNewDocument(false);
    setNotice({ type: "", text: "" });
  };

  const startNewDocument = () => {
    setSelectedDocument(null);
    setDocumentId("");
    setDocumentJson("{\n  \n}");
    setIsNewDocument(true);
    setNotice({ type: "", text: "" });
  };

  const saveDocument = async (event) => {
    event.preventDefault();
    if (!selectedCollection) return;
    let parsed;
    try {
      parsed = JSON.parse(documentJson);
      if (!parsed || typeof parsed !== "object" || Array.isArray(parsed))
        throw new Error("Document data must be a JSON object.");
    } catch (err) {
      setNotice({ type: "error", text: `Invalid JSON: ${err.message}` });
      return;
    }
    if (isNewDocument && !documentId.trim()) {
      setNotice({
        type: "error",
        text: "Enter a document ID, or leave it blank to let Firestore generate one.",
      });
      return;
    }
    setBusy(true);
    try {
      let result;
      if (isNewDocument) {
        result = await apiFetch(
          `/api/admin/collections/${encodeURIComponent(selectedCollection)}/documents`,
          {
            method: "POST",
            body: JSON.stringify({
              id: documentId.trim() || undefined,
              data: parsed,
            }),
          },
        );
        setNotice({
          type: "success",
          text: `Document ${result.id} created successfully.`,
        });
      } else {
        await apiFetch(
          `/api/admin/collections/${encodeURIComponent(selectedCollection)}/documents/${encodeURIComponent(documentId)}`,
          {
            method: "PUT",
            body: JSON.stringify({ data: parsed }),
          },
        );
        setNotice({ type: "success", text: "Document saved successfully." });
      }
      await loadDocuments(selectedCollection);
      if (isNewDocument && result?.id) {
        setSelectedDocument({ id: result.id, data: parsed });
        setDocumentId(result.id);
        setDocumentJson(JSON.stringify(parsed, null, 2));
        setIsNewDocument(false);
      }
    } catch (err) {
      setNotice({ type: "error", text: err.message });
    } finally {
      setBusy(false);
    }
  };

  const deleteDocument = async () => {
    if (
      !selectedDocument ||
      !window.confirm(
        `Delete document “${selectedDocument.id}” from “${selectedCollection}”? This cannot be undone.`,
      )
    )
      return;
    setBusy(true);
    try {
      await apiFetch(
        `/api/admin/collections/${encodeURIComponent(selectedCollection)}/documents/${encodeURIComponent(selectedDocument.id)}`,
        { method: "DELETE" },
      );
      setNotice({ type: "success", text: "Document deleted successfully." });
      await loadDocuments(selectedCollection);
    } catch (err) {
      setNotice({ type: "error", text: err.message });
    } finally {
      setBusy(false);
    }
  };

  const deleteUser = async (user) => {
    if (
      !window.confirm(
        `Delete ${user.name || user.email || user.id}? This removes the Firebase Authentication account and its user profile.`,
      )
    )
      return;
    setBusy(true);
    try {
      await apiFetch(`/api/users/${encodeURIComponent(user.id)}`, {
        method: "DELETE",
      });
      setNotice({ type: "success", text: "User account deleted." });
      await loadUsers();
    } catch (err) {
      setNotice({ type: "error", text: err.message });
    } finally {
      setBusy(false);
    }
  };

  const [editingUser, setEditingUser] = useState(null);
  const [editUserForm, setEditUserForm] = useState({
    name: "",
    email: "",
    role: "Student",
    studentId: "",
    status: "Active",
  });
  const openEditUser = (user) => {
    setEditingUser(user);
    setEditUserForm({
      name:
        user.name || `${user.firstName || ""} ${user.lastName || ""}`.trim(),
      email: user.email || "",
      role: user.role || "Student",
      studentId: user.studentId || "",
      status: user.status || "Active",
    });
  };
  const saveUser = async (event) => {
    event.preventDefault();
    if (!editingUser) return;
    setBusy(true);
    try {
      await apiFetch(`/api/users/${encodeURIComponent(editingUser.id)}`, {
        method: "PATCH",
        body: JSON.stringify(editUserForm),
      });
      setEditingUser(null);
      setNotice({ type: "success", text: "User profile updated." });
      await loadUsers();
    } catch (err) {
      setNotice({ type: "error", text: err.message });
    } finally {
      setBusy(false);
    }
  };

  const instructorsCount = users.filter(
    (u) => String(u.role || "").toLowerCase() === "instructor",
  ).length;
  const studentsCount = users.filter(
    (u) => String(u.role || "").toLowerCase() === "student",
  ).length;

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <aside className="pt-20 fixed inset-y-0 left-0 z-20 hidden w-64 flex-col border-r border-slate-200 bg-white lg:flex">
        <div className="px-4 pt-6 text-xs font-semibold uppercase tracking-widest text-slate-400">
          Workspace
        </div>
        <nav className="mt-2 space-y-1 px-3">
          <button
            onClick={() => setActiveTab("users")}
            className={`flex w-full items-center gap-3 rounded-xl px-3 py-3 text-sm font-semibold ${activeTab === "users" ? "bg-indigo-50 text-indigo-800" : "text-slate-600 hover:bg-slate-50"}`}
          >
            <Users size={18} /> User management
          </button>
          <button
            onClick={() => setActiveTab("database")}
            className={`flex w-full items-center gap-3 rounded-xl px-3 py-3 text-sm font-semibold ${activeTab === "database" ? "bg-indigo-50 text-indigo-800" : "text-slate-600 hover:bg-slate-50"}`}
          >
            <Database size={18} /> Firestore database
          </button>
        </nav>
      </aside>

      <main className="pt-16 lg:pl-64">
        <div className="space-y-6 p-5 sm:p-8">
          {notice.text && (
            <div
              role="status"
              className={`flex items-start gap-3 rounded-xl border px-4 py-3 text-sm ${notice.type === "error" ? "border-rose-200 bg-rose-50 text-rose-800" : "border-emerald-200 bg-emerald-50 text-emerald-800"}`}
            >
              <span className="mt-0.5">
                {notice.type === "error" ? (
                  <X size={17} />
                ) : (
                  <Check size={17} />
                )}
              </span>
              <span className="flex-1">{notice.text}</span>
              <button
                onClick={() => setNotice({ type: "", text: "" })}
                aria-label="Dismiss message"
              >
                <X size={16} />
              </button>
            </div>
          )}
          {error && (
            <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
              {error}{" "}
              <button className="ml-2 underline" onClick={loadUsers}>
                Try again
              </button>
            </div>
          )}

          {activeTab === "users" ? (
            <>
              <section className={`${card} overflow-hidden`}>
                <div className="flex border-b border-slate-100 p-4 sm:px-6 sm:justify-between">
                  <div className="relative max-w-lg">
                    <Search
                      className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
                      size={17}
                    />
                    <input
                      className={`${input} pl-10`}
                      placeholder="Search name, email, role, or ID..."
                      value={userSearch}
                      onChange={(e) => setUserSearch(e.target.value)}
                    />
                  </div>

                  <button
                    className={buttonPrimary}
                    onClick={() => setShowInstructorForm(true)}
                  >
                    <UserPlus size={17} /> Create instructor
                  </button>
                </div>
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[760px] text-left text-sm">
                    <thead className="bg-slate-50 text-xs uppercase tracking-wider text-slate-500">
                      <tr>
                        <th className="px-6 py-3 font-semibold">Account</th>
                        <th className="px-6 py-3 font-semibold">Email</th>
                        <th className="px-6 py-3 font-semibold">Role</th>
                        <th className="px-6 py-3 font-semibold">Status</th>
                        <th className="px-6 py-3 text-right font-semibold">
                          Actions
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {loadingUsers ? (
                        <tr>
                          <td
                            colSpan="5"
                            className="px-6 py-12 text-center text-slate-500"
                          >
                            <LoaderCircle
                              className="mr-2 inline animate-spin"
                              size={18}
                            />{" "}
                            Loading accounts…
                          </td>
                        </tr>
                      ) : filteredUsers.length === 0 ? (
                        <tr>
                          <td
                            colSpan="5"
                            className="px-6 py-12 text-center text-slate-500"
                          >
                            No matching accounts found.
                          </td>
                        </tr>
                      ) : (
                        filteredUsers.map((user) => {
                          const role = user.role || "Unassigned";
                          const status =
                            user.status ||
                            (user.disabled ? "Inactive" : "Active");
                          return (
                            <tr key={user.id} className="hover:bg-slate-50/70">
                              <td className="px-6 py-4">
                                <div className="font-semibold text-slate-800">
                                  {user.name ||
                                    `${user.firstName || ""} ${user.lastName || ""}`.trim() ||
                                    "Unnamed user"}
                                </div>
                                <div className="mt-1 max-w-48 truncate text-xs text-slate-400">
                                  {user.id}
                                </div>
                              </td>
                              <td className="px-6 py-4 text-slate-600">
                                {user.email || "—"}
                              </td>
                              <td className="px-6 py-4">
                                <span
                                  className={`rounded-full px-2.5 py-1 text-xs font-semibold ${String(role).toLowerCase() === "instructor" ? "bg-violet-50 text-violet-700" : String(role).toLowerCase() === "admin" ? "bg-amber-50 text-amber-700" : "bg-slate-100 text-slate-600"}`}
                                >
                                  {role}
                                </span>
                              </td>
                              <td className="px-6 py-4">
                                <span
                                  className={`inline-flex items-center gap-2 text-xs font-semibold ${String(status).toLowerCase() === "active" ? "text-emerald-700" : "text-slate-500"}`}
                                >
                                  <span
                                    className={`h-2 w-2 rounded-full ${String(status).toLowerCase() === "active" ? "bg-emerald-500" : "bg-slate-400"}`}
                                  />
                                  {status}
                                </span>
                              </td>
                              <td className="px-6 py-4">
                                <div className="flex justify-end gap-2">
                                  <button
                                    className="rounded-lg p-2 text-slate-500 hover:bg-indigo-50 hover:text-indigo-700"
                                    title="Edit account"
                                    onClick={() => openEditUser(user)}
                                  >
                                    <Pencil size={16} />
                                  </button>
                                  <button
                                    className="rounded-lg p-2 text-rose-500 hover:bg-rose-50"
                                    title="Delete account"
                                    onClick={() => deleteUser(user)}
                                  >
                                    <Trash2 size={16} />
                                  </button>
                                </div>
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                  </table>
                </div>
                <div className="border-t border-slate-100 px-6 py-4 text-xs text-slate-500">
                  Showing {filteredUsers.length} of {users.length} accounts
                </div>
              </section>
            </>
          ) : (
            <>
              <div className="grid gap-4 md:grid-cols-[minmax(220px,0.75fr)_minmax(0,1.5fr)]">
                <section className={`${card} overflow-hidden`}>
                  <div className="flex items-center justify-between border-b border-slate-100 p-4">
                    <div>
                      <h2 className="font-bold">Collections</h2>
                      <p className="mt-1 text-xs text-slate-500">
                        Firestore collections
                      </p>
                    </div>
                    <button
                      title="Refresh collections"
                      className="rounded-lg p-2 text-slate-500 hover:bg-slate-100"
                      onClick={() => loadCollections()}
                    >
                      <RefreshCw size={16} />
                    </button>
                  </div>
                  <div className="max-h-[65vh] overflow-y-auto p-2">
                    {loadingDatabase && collections.length === 0 ? (
                      <p className="p-4 text-sm text-slate-500">
                        Loading collections…
                      </p>
                    ) : collections.length === 0 ? (
                      <p className="p-4 text-sm text-slate-500">
                        No collections found.
                      </p>
                    ) : (
                      collections.map((collection) => (
                        <button
                          key={collection.id}
                          onClick={() => {
                            setSelectedCollection(collection.id);
                            setSelectedDocument(null);
                            setIsNewDocument(false);
                            loadDocuments(collection.id);
                          }}
                          className={`flex w-full items-center gap-3 rounded-xl px-3 py-3 text-left text-sm ${selectedCollection === collection.id ? "bg-indigo-50 font-semibold text-indigo-800" : "text-slate-700 hover:bg-slate-50"}`}
                        >
                          <FolderOpen size={17} />
                          <span className="min-w-0 flex-1 truncate">
                            {collection.id}
                          </span>
                          <ChevronDown
                            className="-rotate-90 text-slate-400"
                            size={14}
                          />
                        </button>
                      ))
                    )}
                  </div>
                </section>

                <section className={`${card} overflow-hidden`}>
                  <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 p-4 sm:p-5">
                    <div>
                      <h2 className="font-bold">
                        {selectedCollection || "Documents"}
                      </h2>
                      <p className="mt-1 text-xs text-slate-500">
                        {documents.length} document(s) loaded · maximum 250 per
                        collection
                      </p>
                    </div>
                    <button
                      className={buttonPrimary}
                      disabled={!selectedCollection}
                      onClick={startNewDocument}
                    >
                      <FilePlus2 size={16} /> New document
                    </button>
                  </div>
                  <div className="grid md:grid-cols-[minmax(160px,0.8fr)_minmax(0,1.2fr)]">
                    <div className="max-h-[65vh] overflow-y-auto border-b border-slate-100 p-2 md:border-b-0 md:border-r">
                      {loadingDatabase ? (
                        <p className="p-4 text-sm text-slate-500">
                          Loading documents…
                        </p>
                      ) : documents.length === 0 ? (
                        <p className="p-4 text-sm text-slate-500">
                          No documents in this collection.
                        </p>
                      ) : (
                        documents.map((doc) => (
                          <button
                            key={doc.id}
                            onClick={() => openDocument(doc)}
                            className={`mb-1 flex w-full items-start gap-2 rounded-xl px-3 py-3 text-left text-sm ${selectedDocument?.id === doc.id && !isNewDocument ? "bg-indigo-50 text-indigo-800" : "hover:bg-slate-50"}`}
                          >
                            <Database size={15} className="mt-0.5 shrink-0" />
                            <span className="min-w-0 break-all font-medium">
                              {doc.id}
                            </span>
                          </button>
                        ))
                      )}
                    </div>
                    <div className="min-w-0 p-4 sm:p-5">
                      {!selectedDocument && !isNewDocument ? (
                        <div className="flex min-h-64 flex-col items-center justify-center text-center">
                          <div className="grid h-12 w-12 place-items-center rounded-2xl bg-slate-100 text-slate-500">
                            <FolderOpen size={22} />
                          </div>
                          <p className="mt-3 font-semibold">
                            Select a document
                          </p>
                          <p className="mt-1 max-w-xs text-sm text-slate-500">
                            Choose a document on the left to inspect or edit its
                            fields.
                          </p>
                        </div>
                      ) : (
                        <form onSubmit={saveDocument} className="space-y-4">
                          <div>
                            <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
                              Document ID
                            </label>
                            <input
                              className={input}
                              value={documentId}
                              onChange={(e) => setDocumentId(e.target.value)}
                              disabled={!isNewDocument}
                              placeholder="Firestore document ID"
                              required={isNewDocument}
                            />
                            <p className="mt-1.5 text-xs text-slate-400">
                              Edit the fields below as valid JSON. Replacing a
                              document overwrites its current fields.
                            </p>
                          </div>
                          <div>
                            <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-slate-500">
                              Document data (JSON)
                            </label>
                            <textarea
                              spellCheck="false"
                              className={`${input} min-h-[300px] resize-y font-mono text-xs leading-5`}
                              value={documentJson}
                              onChange={(e) => setDocumentJson(e.target.value)}
                            />
                          </div>
                          <div className="flex flex-wrap items-center justify-between gap-2">
                            <div>
                              {selectedDocument && !isNewDocument && (
                                <button
                                  type="button"
                                  className="inline-flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-semibold text-rose-600 hover:bg-rose-50"
                                  onClick={deleteDocument}
                                  disabled={busy}
                                >
                                  <Trash2 size={16} /> Delete
                                </button>
                              )}
                            </div>
                            <div className="flex gap-2">
                              <button
                                type="button"
                                className={buttonSecondary}
                                onClick={() => {
                                  if (selectedDocument && !isNewDocument)
                                    openDocument(selectedDocument);
                                  else startNewDocument();
                                }}
                              >
                                Reset
                              </button>
                              <button
                                type="submit"
                                className={buttonPrimary}
                                disabled={busy}
                              >
                                {busy ? (
                                  <LoaderCircle
                                    size={16}
                                    className="animate-spin"
                                  />
                                ) : (
                                  <Check size={16} />
                                )}
                                {isNewDocument
                                  ? "Create document"
                                  : "Save changes"}
                              </button>
                            </div>
                          </div>
                        </form>
                      )}
                    </div>
                  </div>
                </section>
              </div>
            </>
          )}
        </div>
      </main>

      {showInstructorForm && (
        <Modal
          title="Create instructor account"
          onClose={() => setShowInstructorForm(false)}
        >
          <form onSubmit={submitInstructor} className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field label="First name" required>
                <input
                  className={input}
                  value={instructor.firstName}
                  onChange={(e) =>
                    setInstructor({ ...instructor, firstName: e.target.value })
                  }
                  required
                  autoComplete="given-name"
                />
              </Field>
              <Field label="Last name" required>
                <input
                  className={input}
                  value={instructor.lastName}
                  onChange={(e) =>
                    setInstructor({ ...instructor, lastName: e.target.value })
                  }
                  required
                  autoComplete="family-name"
                />
              </Field>
            </div>
            <Field label="Email address" required>
              <input
                className={input}
                type="email"
                value={instructor.email}
                onChange={(e) =>
                  setInstructor({ ...instructor, email: e.target.value })
                }
                required
                autoComplete="email"
              />
            </Field>
            <Field label="Temporary password" required>
              <input
                className={input}
                type="password"
                minLength={6}
                value={instructor.password}
                onChange={(e) =>
                  setInstructor({ ...instructor, password: e.target.value })
                }
                required
                autoComplete="new-password"
              />
              <p className="mt-1 text-xs text-slate-400">
                At least 6 characters. Share credentials securely with the
                instructor.
              </p>
            </Field>
            <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
              <button
                type="button"
                className={buttonSecondary}
                onClick={() => setShowInstructorForm(false)}
              >
                Cancel
              </button>
              <button type="submit" className={buttonPrimary} disabled={busy}>
                {busy ? (
                  <LoaderCircle size={16} className="animate-spin" />
                ) : (
                  <UserPlus size={16} />
                )}
                Create account
              </button>
            </div>
          </form>
        </Modal>
      )}

      {editingUser && (
        <Modal title="Edit user account" onClose={() => setEditingUser(null)}>
          <form onSubmit={saveUser} className="space-y-4">
            <Field label="Full name" required>
              <input
                className={input}
                value={editUserForm.name}
                onChange={(e) =>
                  setEditUserForm({ ...editUserForm, name: e.target.value })
                }
                required
              />
            </Field>
            <Field label="Email address" required>
              <input
                className={input}
                type="email"
                value={editUserForm.email}
                onChange={(e) =>
                  setEditUserForm({ ...editUserForm, email: e.target.value })
                }
                required
              />
            </Field>
            <Field label="Role">
              <select
                className={input}
                value={editUserForm.role}
                onChange={(e) =>
                  setEditUserForm({ ...editUserForm, role: e.target.value })
                }
              >
                <option value="Student">Student</option>
                <option value="Instructor">Instructor</option>
                <option value="Admin">Admin</option>
              </select>
              <p className="mt-1 text-xs text-slate-400">
                Only grant Admin to trusted administrators.
              </p>
            </Field>
            <Field label="Student ID">
              <input
                className={input}
                value={editUserForm.studentId}
                onChange={(e) =>
                  setEditUserForm({
                    ...editUserForm,
                    studentId: e.target.value,
                  })
                }
              />
            </Field>
            <Field label="Status">
              <select
                className={input}
                value={editUserForm.status}
                onChange={(e) =>
                  setEditUserForm({ ...editUserForm, status: e.target.value })
                }
              >
                <option>Active</option>
                <option>Inactive</option>
              </select>
            </Field>
            <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
              <button
                type="button"
                className={buttonSecondary}
                onClick={() => setEditingUser(null)}
              >
                Cancel
              </button>
              <button className={buttonPrimary} disabled={busy}>
                {busy ? "Saving…" : "Save changes"}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
}

function StatCard({ title, value, icon }) {
  return (
    <div className={`${card} flex items-center gap-4 p-5`}>
      <div className="grid h-11 w-11 place-items-center rounded-xl bg-indigo-50 text-indigo-700">
        {icon}
      </div>
      <div>
        <p className="text-sm text-slate-500">{title}</p>
        <p className="mt-0.5 text-2xl font-bold tracking-tight">{value}</p>
      </div>
    </div>
  );
}

function Modal({ title, onClose, children }) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-950/50 p-4 backdrop-blur-sm"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="my-auto w-full max-w-lg rounded-2xl bg-white p-5 shadow-2xl sm:p-6"
      >
        <div className="mb-5 flex items-center justify-between gap-4">
          <h2 className="text-lg font-bold">{title}</h2>
          <button
            type="button"
            className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
            onClick={onClose}
            aria-label="Close"
          >
            <X size={19} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

function Field({ label, required, children }) {
  return (
    <div>
      <label className="mb-1.5 block text-sm font-semibold text-slate-700">
        {label}
        {required && <span className="ml-1 text-rose-500">*</span>}
      </label>
      {children}
    </div>
  );
}
