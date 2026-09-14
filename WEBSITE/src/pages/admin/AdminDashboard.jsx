import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { collection, getDocs } from "firebase/firestore";
import { db } from "../../config/firebase-config";
import { User, Users, Pencil, Play, Search, Trash2, Plus } from "lucide-react";

export default function AdminDashboard() {
  const navigate = useNavigate();

  const [users, setUsers] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [loading, setLoading] = useState(true);
  const [sections, setSections] = useState({});
  const [error, setError] = useState("");

  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);

  const [selectedUser, setSelectedUser] = useState(null);

  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const [formData, setFormData] = useState({
    name: "",
    email: "",
    password: "",
    role: "Student",
    studentId: "",
    status: "Active",
  });

  const openCreateModal = () => {
    setFormData({
      name: "",
      email: "",
      password: "",
      role: "Student",
      studentId: "",
      status: "Active",
    });

    setShowCreateModal(true);
  };

  const openEditModal = (user) => {
    setSelectedUser(user);

    setFormData({
      name: getFullName(user),
      email: user.email || "",
      password: "",
      role: user.role || "Student",
      studentId: user.studentId || "",
      status: user.status || "Active",
    });

    setShowEditModal(true);
  };

  const openDeleteModal = (user) => {
    setSelectedUser(user);
    setShowDeleteModal(true);
  };

  const handleFormChange = (e) => {
    const { name, value } = e.target;

    setFormData((previous) => ({
      ...previous,
      [name]: value,
    }));
  };
  const getUsers = async () => {
    try {
      setLoading(true);

      const response = await fetch("http://localhost:5000/api/users");

      if (!response.ok) {
        throw new Error("Failed to retrieve users.");
      }

      const data = await response.json();

      setUsers(data);
    } catch (error) {
      console.error("Error getting users:", error);
      setError("Unable to load users.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    getUsers();
    getUserSections();
  }, []);

  const handleCreateUser = async (e) => {
    e.preventDefault();

    try {
      setSaving(true);

      const response = await fetch("http://localhost:5000/api/users", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(formData),
      });

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || "Unable to create user.");
      }

      setShowCreateModal(false);

      await getUsers();
    } catch (error) {
      console.error("Error creating user:", error);
      alert(error.message);
    } finally {
      setSaving(false);
    }
  };

  const handleEditUser = async (e) => {
    e.preventDefault();

    if (!selectedUser) return;

    try {
      setSaving(true);

      const response = await fetch(
        `http://localhost:5000/api/users/${selectedUser.id}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            name: formData.name,
            email: formData.email,
            role: formData.role,
            studentId: formData.studentId,
            status: formData.status,
          }),
        },
      );

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || "Unable to update user.");
      }

      setShowEditModal(false);
      setSelectedUser(null);

      await getUsers();
    } catch (error) {
      console.error("Error updating user:", error);
      alert(error.message);
    } finally {
      setSaving(false);
    }
  };
  const handleDeleteUser = async () => {
    if (!selectedUser) return;

    try {
      setDeleting(true);

      const response = await fetch(
        `http://localhost:5000/api/users/${selectedUser.id}`,
        {
          method: "DELETE",
        },
      );

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || "Unable to delete user.");
      }

      setUsers((previous) =>
        previous.filter((user) => user.id !== selectedUser.id),
      );

      setShowDeleteModal(false);
      setSelectedUser(null);
    } catch (error) {
      console.error("Error deleting user:", error);
      alert(error.message);
    } finally {
      setDeleting(false);
    }
  };

  const filteredUsers = users.filter((user) => {
    const search = searchTerm.toLowerCase().trim();

    if (!search) {
      return true;
    }

    const fullName = `
      ${user.firstName || ""}
      ${user.lastName || ""}
    `.toLowerCase();

    const email = (user.email || "").toLowerCase();

    const userId = (user.userId || user.studentId || user.id || "")
      .toString()
      .toLowerCase();

    return (
      fullName.includes(search) ||
      email.includes(search) ||
      userId.includes(search)
    );
  });

  const getFullName = (user) => {
    const name = `${user.firstName || ""} ${user.lastName || ""}`.trim();

    return name || user.name || "Unnamed User";
  };

  const getUserId = (user, index) => {
    return (
      user.userId ||
      user.studentId ||
      user.employeeId ||
      String(index + 1).padStart(6, "0")
    );
  };

  const getSections = (user) => {
    return sections[user.id] || [];
  };

  const getUserSections = async () => {
    try {
      const blocksSnapshot = await getDocs(collection(db, "block"));

      const sectionMap = {};

      blocksSnapshot.docs.forEach((blockDoc) => {
        const blockData = blockDoc.data();

        const classCode = blockData.classCode;

        if (!classCode) {
          return;
        }

        const studentIds = blockData.studentIds || [];

        studentIds.forEach((studentId) => {
          if (!sectionMap[studentId]) {
            sectionMap[studentId] = [];
          }

          if (!sectionMap[studentId].includes(classCode)) {
            sectionMap[studentId].push(classCode);
          }
        });

        const instructorId = blockData.instructorId;

        if (instructorId) {
          if (!sectionMap[instructorId]) {
            sectionMap[instructorId] = [];
          }

          if (!sectionMap[instructorId].includes(classCode)) {
            sectionMap[instructorId].push(classCode);
          }
        }
      });

      setSections(sectionMap);
    } catch (error) {
      console.error("Error getting sections:", error);
    }
  };

  const getStatus = (user) => {
    return user.status || "Active";
  };

  return (
    <>
      <div className="font-google min-h-screen bg-white text-black">
        <nav className="px-6 pt-24 py-5">
          <div className="flex items-center gap-8">
            <button
              className="
              flex
              items-center
              gap-3
              text-2xl
              text-black
            "
            >
              <User size={32} />
              Users
            </button>

            <button
              onClick={() => navigate("/admin/classes")}
              className="
              flex
              items-center
              gap-3
              text-2xl
              text-gray-500
              hover:text-black
              transition
            "
            >
              <Users size={32} />
              Classes
            </button>

            <button
              onClick={() => navigate("/admin/equipment")}
              className="
              flex
              items-center
              gap-3
              text-2xl
              text-gray-500
              hover:text-black
              transition
            "
            >
              <Pencil size={32} />
              VR Equipment
            </button>

            <button
              onClick={() => navigate("/admin/logs")}
              className="
              flex
              items-center
              gap-3
              text-2xl
              text-gray-500
              hover:text-black
              transition
            "
            >
              <Play size={32} />
              Logs
            </button>
          </div>
        </nav>

        <section className="px-7 pt-11 pb-10">
          <div
            className="
            border
            border-gray-400
            rounded-xl
            overflow-hidden
            shadow-sm
          "
          >
            <div
              className="
              px-6
              py-6
              flex
              items-center
              justify-between
              border-b
              border-gray-200
            "
            >
              <div className="relative w-[485px]">
                <Search
                  size={25}
                  className="
                  absolute
                  left-4
                  top-1/2
                  -translate-y-1/2
                  text-gray-400
                "
                />

                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder="Search by name or ID..."
                  className="
                  w-full
                  h-12
                  pl-12
                  pr-4
                  border
                  border-gray-300
                  rounded-lg
                  text-lg
                  outline-none
                  focus:ring-2
                  focus:ring-indigo-500
                "
                />
              </div>

              <button
                onClick={openCreateModal}
                className="
                flex
                items-center
                gap-2
                px-5
                py-3
                bg-indigo-800
                hover:bg-indigo-700
                text-white
                rounded-lg
                text-lg
                transition
              "
              >
                <Plus size={22} />
                Create New User
              </button>
            </div>

            <div
              className="
              grid
              grid-cols-[1fr_1.5fr_2fr_1fr_1fr_1.5fr_1fr]
              px-6
              py-4
              bg-gray-50
              border-b
              border-gray-200
              text-sm
              font-medium
              tracking-wide
            "
            >
              <span>USER ID</span>
              <span>NAME</span>
              <span>EMAIL</span>
              <span>ROLE</span>
              <span>STATUS</span>
              <span>SECTION</span>
              <span className="text-center">ACTIONS</span>
            </div>

            {loading && (
              <div className="py-12 text-center text-gray-500">
                Loading users...
              </div>
            )}

            {!loading && filteredUsers.length === 0 && (
              <div className="py-12 text-center text-gray-500">
                {searchTerm ? "No users match your search." : "No users found."}
              </div>
            )}

            {!loading &&
              filteredUsers.map((user, index) => {
                const role = user.role || "Student";

                const status = getStatus(user);

                const sections = getSections(user);

                return (
                  <div
                    key={user.id}
                    className="
                    grid
                    grid-cols-[1fr_1.5fr_2fr_1fr_1fr_1.5fr_1fr]
                    items-center
                    px-6
                    py-5
                    border-b
                    border-gray-200
                    hover:bg-gray-50
                    transition
                  "
                  >
                    <span className="text-base">{getUserId(user, index)}</span>

                    <span className="text-base">{getFullName(user)}</span>

                    <span className="text-base truncate pr-3">
                      {user.email || "N/A"}
                    </span>

                    <span>
                      <span
                        className={`
                        inline-flex
                        px-3
                        py-1
                        rounded-full
                        text-sm
                        ${
                          role.toLowerCase() === "instructor"
                            ? "bg-purple-100 text-purple-700"
                            : "bg-green-100 text-green-700"
                        }
                      `}
                      >
                        {role}
                      </span>
                    </span>

                    <span>
                      <span
                        className={`
                        inline-flex
                        items-center
                        gap-2
                        px-3
                        py-1
                        rounded-full
                        text-sm
                        ${
                          status.toLowerCase() === "active"
                            ? "bg-green-50 text-green-700"
                            : "bg-gray-100 text-gray-700"
                        }
                      `}
                      >
                        <span
                          className={`
                          w-2
                          h-2
                          rounded-full
                          ${
                            status.toLowerCase() === "active"
                              ? "bg-green-500"
                              : "bg-gray-400"
                          }
                        `}
                        />

                        {status}
                      </span>
                    </span>

                    <div className="flex flex-wrap gap-1">
                      {sections.length === 0 ? (
                        <span className="text-gray-400">—</span>
                      ) : (
                        sections.map((section, sectionIndex) => (
                          <span
                            key={sectionIndex}
                            className="
                            px-3
                            py-1
                            bg-gray-300
                            rounded-md
                            text-sm
                          "
                          >
                            {section}
                          </span>
                        ))
                      )}
                    </div>

                    <div className="flex items-center justify-center gap-5">
                      <button
                        onClick={() => openEditModal(user)}
                        title="Edit User"
                        className="
                        hover:text-indigo-800
                        transition
                      "
                      >
                        <Pencil size={22} />
                      </button>

                      <button
                        onClick={() => openDeleteModal(user)}
                        title="Delete User"
                        className="
                        text-red-500
                        hover:text-red-700
                        transition
                      "
                      >
                        <Trash2 size={22} />
                      </button>
                    </div>
                  </div>
                );
              })}

            {!loading && (
              <div className="px-6 py-4 text-gray-600">
                Showing {filteredUsers.length} of {users.length} users
              </div>
            )}
          </div>
        </section>
      </div>

      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-[450px] rounded-xl bg-white p-6 shadow-2xl">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-3xl font-normal">Create New User</h2>

              <button
                onClick={() => setShowCreateModal(false)}
                className="text-2xl text-gray-400 hover:text-gray-700"
              >
                ×
              </button>
            </div>

            <form onSubmit={handleCreateUser}>
              <label className="mb-1 block text-sm text-gray-700">Name *</label>

              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleFormChange}
                required
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              />

              <label className="mb-1 block text-sm text-gray-700">
                Email *
              </label>

              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleFormChange}
                required
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              />

              <label className="mb-1 block text-sm text-gray-700">
                Password *
              </label>

              <input
                type="password"
                name="password"
                value={formData.password}
                onChange={handleFormChange}
                required
                minLength={6}
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              />

              <label className="mb-1 block text-sm text-gray-700">Role *</label>

              <select
                name="role"
                value={formData.role}
                onChange={handleFormChange}
                required
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 bg-white px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="Student">Student</option>
                <option value="Instructor">Instructor</option>
              </select>

              <label className="mb-1 block text-sm text-gray-700">
                Student ID
              </label>

              <input
                type="text"
                name="studentId"
                value={formData.studentId}
                onChange={handleFormChange}
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              />

              <label className="mb-1 block text-sm text-gray-700">Status</label>

              <select
                name="status"
                value={formData.status}
                onChange={handleFormChange}
                className="h-11 w-full rounded-lg border border-gray-300 bg-white px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>

              <div className="mt-8 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  className="rounded-lg bg-gray-100 px-4 py-2 text-sm text-gray-700 hover:bg-gray-200"
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  disabled={saving}
                  className="rounded-lg bg-indigo-800 px-4 py-2 text-sm text-white hover:bg-indigo-700 disabled:opacity-50"
                >
                  {saving ? "Creating..." : "Create User"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showEditModal && selectedUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-[450px] rounded-xl bg-white p-6 shadow-2xl">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-3xl font-normal">Edit User</h2>

              <button
                onClick={() => {
                  setShowEditModal(false);
                  setSelectedUser(null);
                }}
                className="text-2xl text-gray-400 hover:text-gray-700"
              >
                ×
              </button>
            </div>

            <form onSubmit={handleEditUser}>
              <label className="mb-1 block text-sm text-gray-700">Name *</label>

              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleFormChange}
                required
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              />

              <label className="mb-1 block text-sm text-gray-700">
                Email *
              </label>

              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleFormChange}
                required
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              />

              <label className="mb-1 block text-sm text-gray-700">Role *</label>

              <select
                name="role"
                value={formData.role}
                onChange={handleFormChange}
                required
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 bg-white px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="Student">Student</option>
                <option value="Instructor">Instructor</option>
              </select>

              <label className="mb-1 block text-sm text-gray-700">
                Student ID
              </label>

              <input
                type="text"
                name="studentId"
                value={formData.studentId}
                onChange={handleFormChange}
                className="mb-4 h-11 w-full rounded-lg border border-gray-300 px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              />

              <label className="mb-1 block text-sm text-gray-700">Status</label>

              <select
                name="status"
                value={formData.status}
                onChange={handleFormChange}
                className="h-11 w-full rounded-lg border border-gray-300 bg-white px-3 outline-none focus:ring-2 focus:ring-indigo-500"
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>

              <div className="mt-8 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => {
                    setShowEditModal(false);
                    setSelectedUser(null);
                  }}
                  className="rounded-lg bg-gray-100 px-4 py-2 text-sm text-gray-700 hover:bg-gray-200"
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  disabled={saving}
                  className="rounded-lg bg-indigo-800 px-4 py-2 text-sm text-white hover:bg-indigo-700 disabled:opacity-50"
                >
                  {saving ? "Saving..." : "Save Changes"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showDeleteModal && selectedUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="w-[450px] rounded-xl bg-white p-6 shadow-2xl">
            <h2 className="text-3xl font-normal">Delete User</h2>

            <p className="mt-1 text-base leading-6 text-gray-600">
              Are you sure you want to delete{" "}
              <span className="font-medium text-gray-700">
                {getFullName(selectedUser)}
              </span>
              ? This action cannot be undone and will remove all associated
              laboratory access.
            </p>

            <div className="mt-7 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => {
                  setShowDeleteModal(false);
                  setSelectedUser(null);
                }}
                className="rounded-lg bg-gray-100 px-4 py-2 text-sm text-gray-700 hover:bg-gray-200"
              >
                Cancel
              </button>

              <button
                type="button"
                onClick={handleDeleteUser}
                disabled={deleting}
                className="rounded-lg bg-red-600 px-4 py-2 text-sm text-white hover:bg-red-700 disabled:opacity-50"
              >
                {deleting ? "Deleting..." : "Delete User"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
