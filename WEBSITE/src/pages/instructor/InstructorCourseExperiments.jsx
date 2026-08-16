import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  doc,
  getDoc,
  collection,
  getDocs,
  query,
  where,
  addDoc,
  updateDoc,
  deleteDoc,
  serverTimestamp,
} from "firebase/firestore";

import { db } from "../../config/firebase-config";

import {
  FileText,
  User,
  Search,
  Play,
  Pencil,
  Trash2,
  Plus,
  ArrowLeft,
  UserPlus,
  Users,
  Settings,
} from "lucide-react";

export default function InstructorCourseExperiments() {
  const navigate = useNavigate();
  const { blockId } = useParams();
  const [course, setCourse] = useState(null);
  const [experiments, setExperiments] = useState([]);
  const [experimentSearch, setExperimentSearch] = useState("");
  const [students, setStudents] = useState([]);
  const [studentSearch, setStudentSearch] = useState("");
  const [activeTab, setActiveTab] = useState("experiments");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showManageGroupModal, setShowManageGroupModal] = useState(false);
  const [showCreateGroupModal, setShowCreateGroupModal] = useState(false);
  const [showEditGroupModal, setShowEditGroupModal] = useState(false);

  const [selectedStudents, setSelectedStudents] = useState([]);
  const [groupName, setGroupName] = useState("");
  const [editingGroup, setEditingGroup] = useState(null);

  const [availableStudents, setAvailableStudents] = useState([]);
  const [groups, setGroups] = useState([]);

  // Get groups function
  const getGroups = async () => {
    try {
      const groupsRef = collection(db, "group");

      const q = query(groupsRef, where("blockId", "==", blockId));

      const snapshot = await getDocs(q);

      const groupList = snapshot.docs.map((groupDoc) => ({
        id: groupDoc.id,
        ...groupDoc.data(),
      }));

      setGroups(groupList);
    } catch (error) {
      console.error("Error getting groups:", error);
    }
  };

  useEffect(() => {
    const getData = async () => {
      try {
        setLoading(true);
        setError("");

        const courseRef = doc(db, "block", blockId);

        const courseSnapshot = await getDoc(courseRef);

        if (!courseSnapshot.exists()) {
          setError("Course not found.");
          return;
        }

        const courseData = courseSnapshot.data();

        const courseObject = {
          id: courseSnapshot.id,
          ...courseData,
        };

        setCourse(courseObject);

        const experimentsRef = collection(db, "experiment");

        const experimentQuery = query(
          experimentsRef,
          where("blockId", "==", blockId),
        );

        const experimentSnapshot = await getDocs(experimentQuery);

        const experimentList = experimentSnapshot.docs.map((experimentDoc) => ({
          id: experimentDoc.id,
          ...experimentDoc.data(),
        }));

        setExperiments(experimentList);

        const studentIds = courseData.studentIds || [];

        if (studentIds.length === 0) {
          setStudents([]);
        } else {
          const studentPromises = studentIds.map(async (studentId) => {
            try {
              const studentRef = doc(db, "user", studentId);

              const studentSnapshot = await getDoc(studentRef);

              if (!studentSnapshot.exists()) {
                return null;
              }

              return {
                id: studentSnapshot.id,
                ...studentSnapshot.data(),
              };
            } catch (error) {
              console.error(`Error getting student ${studentId}:`, error);

              return null;
            }
          });

          const studentResults = await Promise.all(studentPromises);

          setStudents(studentResults.filter((student) => student !== null));
        }
        getGroups();
      } catch (error) {
        console.error("Error getting course data:", error);

        setError("Unable to load course information.");
      } finally {
        setLoading(false);
      }
    };

    if (blockId) {
      getData();
    }
  }, [blockId]);

  const filteredExperiments = experiments.filter((experiment) => {
    const searchTerm = experimentSearch.toLowerCase().trim();

    return (
      experiment.name?.toLowerCase().includes(searchTerm) ||
      experiment.experimentName?.toLowerCase().includes(searchTerm) ||
      experiment.description?.toLowerCase().includes(searchTerm) ||
      experiment.environment?.toLowerCase().includes(searchTerm)
    );
  });

  const filteredStudents = students.filter((student) => {
    const searchTerm = studentSearch.toLowerCase().trim();

    const fullName = `${student.firstName || ""} ${
      student.lastName || ""
    }`.toLowerCase();

    const email = (student.email || "").toLowerCase();

    return fullName.includes(searchTerm) || email.includes(searchTerm);
  });

  const formatDate = (timestamp) => {
    if (!timestamp) {
      return "N/A";
    }

    if (timestamp.toDate) {
      return timestamp.toDate().toLocaleDateString("en-US", {
        month: "2-digit",
        day: "2-digit",
        year: "numeric",
      });
    }

    return "N/A";
  };

  const handleDelete = async (experimentId) => {
    const confirmed = window.confirm(
      "Are you sure you want to delete this experiment?",
    );

    if (!confirmed) {
      return;
    }

    try {
      // Add deleteDoc() here when ready.

      console.log("Delete experiment:", experimentId);
    } catch (error) {
      console.error("Error deleting experiment:", error);
    }
  };

  if (loading) {
    return (
      <div
        className="
          font-google
          min-h-screen
          flex
          items-center
          justify-center
        "
      >
        <p className="text-gray-500">Loading course...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className="
          font-google
          min-h-screen
          flex
          items-center
          justify-center
        "
      >
        <p className="text-red-500">{error}</p>
      </div>
    );
  }

  const openCreateGroup = () => {
    setGroupName("");
    setSelectedStudents([]);
    setShowCreateGroupModal(true);
  };

  const openEditGroup = (group) => {
    setEditingGroup(group);

    setGroupName(group.name);

    setSelectedStudents(group.studentIds || []);

    setShowManageGroupModal(false);
    setShowEditGroupModal(true);
  };

  // Get student's group function
  const getStudentGroup = (studentId) => {
    const group = groups.find((group) => group.studentIds?.includes(studentId));

    return group?.groupName || "Unassigned";
  };

  // Create group function
  const handleCreateGroup = async () => {
    if (!groupName.trim()) {
      setError("Please enter a group name.");
      return;
    }

    if (selectedStudents.length === 0) {
      setError("Please select at least one student.");
      return;
    }

    try {
      const groupsRef = collection(db, "group");

      await addDoc(groupsRef, {
        groupName: groupName.trim(),
        blockId: blockId,
        studentIds: selectedStudents,
        createdAt: serverTimestamp(),
        updatedAt: serverTimestamp(),
      });

      setGroupName("");
      setSelectedStudents([]);
      setShowCreateGroupModal(false);

      await getGroups();
    } catch (error) {
      console.error("Error creating group:", error);
      setError("Unable to create group.");
    }
  };

  // Edit group function
  const handleEditGroup = async (group) => {
    setEditingGroup(group);

    setGroupName(group.name);
    setSelectedStudents(group.studentIds || []);

    setShowEditGroupModal(true);
  };

  // Save group edit function
  const handleSaveGroupChanges = async () => {
    if (!editingGroup) {
      return;
    }

    if (!groupName.trim()) {
      setError("Please enter a group name.");
      return;
    }

    try {
      const groupRef = doc(db, "group", editingGroup.id);

      await updateDoc(groupRef, {
        groupName: groupName.trim(),
        studentIds: selectedStudents,
        updatedAt: serverTimestamp(),
      });

      setShowEditGroupModal(false);
      setEditingGroup(null);
      setGroupName("");
      setSelectedStudents([]);

      await getGroups();
    } catch (error) {
      console.error("Error updating group:", error);
      setError("Unable to update group.");
    }
  };

  // Delete group function
  const handleDeleteGroup = async (groupId) => {
    const confirmed = window.confirm(
      "Are you sure you want to delete this group?",
    );

    if (!confirmed) {
      return;
    }

    try {
      const groupRef = doc(db, "group", groupId);

      await deleteDoc(groupRef);

      await getGroups();
    } catch (error) {
      console.error("Error deleting group:", error);
      setError("Unable to delete group.");
    }
  };

  // Student selection function
  const handleStudentSelection = (studentId) => {
    setSelectedStudents((current) => {
      if (current.includes(studentId)) {
        return current.filter((id) => id !== studentId);
      }

      return [...current, studentId];
    });
  };

  return (
    <div
      className="
        font-google
        min-h-screen
        text-black
        bg-white
      "
    >
      <div
        className="
          px-6
          pt-24
          pb-4
        "
      >
        <button
          onClick={() => navigate("/instructor")}
          className="
            flex
            items-center
            gap-2
            text-gray-600
            hover:text-black
            text-lg
            transition
          "
        >
          <ArrowLeft size={20} />
          Back to Home
        </button>
      </div>

      <div
        className="
          px-6
          border-b
          border-gray-300
        "
      >
        <div className="flex gap-8">
          <button
            onClick={() => setActiveTab("experiments")}
            className={`
              flex
              items-center
              gap-3
              pb-4
              text-xl
              transition
              ${
                activeTab === "experiments"
                  ? `
                    border-b-2
                    border-black
                    text-black
                  `
                  : `
                    text-gray-500
                    hover:text-black
                  `
              }
            `}
          >
            <FileText size={24} />
            Experiments
          </button>

          <button
            onClick={() => setActiveTab("students")}
            className={`
              flex
              items-center
              gap-3
              pb-4
              text-xl
              transition
              ${
                activeTab === "students"
                  ? `
                    border-b-2
                    border-black
                    text-black
                  `
                  : `
                    text-gray-500
                    hover:text-black
                  `
              }
            `}
          >
            <User size={23} />
            Students
          </button>
        </div>
      </div>

      {activeTab === "experiments" && (
        <section className="px-5 py-9">
          <div
            className="
              flex
              items-center
              justify-between
              mb-10
            "
          >
            <button
              onClick={() =>
                navigate(`/instructor/course/${blockId}/experiment/create`)
              }
              className="
                flex
                items-center
                gap-3
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
              <Plus size={24} />
              New Experiment
            </button>

            <p
              className="
                text-2xl
                text-gray-600
              "
            >
              Total Experiments:{" "}
              <span className="font-medium">{experiments.length}</span>
            </p>
          </div>

          <div
            className="
              relative
              mb-4
            "
          >
            <Search
              size={28}
              className="
                absolute
                left-4
                top-1/2
                -translate-y-1/2
                text-gray-600
              "
            />

            <input
              type="text"
              value={experimentSearch}
              onChange={(e) => setExperimentSearch(e.target.value)}
              placeholder="Search Experiments"
              className="
                w-full
                h-16
                pl-16
                pr-4
                bg-gray-200
                border
                border-gray-300
                rounded-2xl
                text-xl
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />
          </div>

          <div className="space-y-4">
            {filteredExperiments.length === 0 && (
              <div
                className="
                  py-10
                  text-center
                  text-gray-500
                "
              >
                {experimentSearch
                  ? "No experiments match your search."
                  : "No experiments have been created yet."}
              </div>
            )}

            {filteredExperiments.map((experiment) => (
              <div
                key={experiment.id}
                className="
                    border
                    border-gray-300
                    rounded-2xl
                    px-5
                    py-5
                    hover:shadow-sm
                    transition
                  "
              >
                <div
                  className="
                      flex
                      items-start
                      justify-between
                      gap-4
                    "
                >
                  <div className="flex-1">
                    <div
                      className="
                          flex
                          items-center
                          gap-4
                          flex-wrap
                        "
                    >
                      <h2
                        className="
                            text-2xl
                            font-semibold
                          "
                      >
                        {experiment.name || experiment.experimentName}
                      </h2>

                      <span
                        className={`
                            px-3
                            py-1
                            rounded-full
                            text-sm
                            ${
                              experiment.status?.toLowerCase() === "published"
                                ? `
                                  bg-green-100
                                  text-green-700
                                `
                                : `
                                  bg-blue-100
                                  text-blue-700
                                `
                            }
                          `}
                      >
                        {experiment.status || "Available"}
                      </span>
                    </div>
                    <p
                      className="
                          text-xl
                          text-gray-600
                          mt-3
                        "
                    >
                      {experiment.description || "No description available."}
                    </p>

                    <div
                      className="
                          flex
                          flex-wrap
                          gap-x-10
                          gap-y-2
                          mt-4
                          text-gray-600
                        "
                    >
                      <span>
                        Environment: {experiment.environment || "N/A"}
                      </span>

                      <span>
                        Stimuli:{" "}
                        {Array.isArray(experiment.stimuli)
                          ? experiment.stimuli.length
                          : (experiment.stimuli ?? 0)}
                      </span>

                      <span>Duration: {experiment.duration ?? 0} mins</span>

                      <span>Modified: {formatDate(experiment.updatedAt)}</span>
                    </div>
                  </div>

                  <div
                    className="
                        flex
                        items-center
                        gap-6
                      "
                  >
                    <button
                      onClick={() =>
                        navigate(`/instructor/experiment/${experiment.id}`)
                      }
                      title="View Experiment"
                      className="
                          hover:text-indigo-800
                          transition
                        "
                    >
                      <Play size={30} strokeWidth={2} />
                    </button>

                    <button
                      onClick={() =>
                        navigate(
                          `/instructor/course/${blockId}/experiment/${experiment.id}/edit`,
                        )
                      }
                      title="Edit Experiment"
                      className="
                          hover:text-indigo-800
                          transition
                        "
                    >
                      <Pencil size={30} strokeWidth={2} />
                    </button>

                    <button
                      onClick={() => handleDelete(experiment.id)}
                      title="Delete Experiment"
                      className="
                          text-red-500
                          hover:text-red-700
                          transition
                        "
                    >
                      <Trash2 size={30} strokeWidth={2} />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {activeTab === "students" && (
        <section
          className="
            max-w-4xl
            mx-auto
            px-5
            py-6
          "
        >
          <div
            className="
              relative
              mb-3
            "
          >
            <Search
              size={28}
              className="
                absolute
                left-4
                top-1/2
                -translate-y-1/2
                text-gray-600
              "
            />

            <input
              type="text"
              value={studentSearch}
              onChange={(e) => setStudentSearch(e.target.value)}
              placeholder="Search students"
              className="
                w-full
                h-16
                pl-16
                pr-4
                bg-gray-200
                border
                border-gray-300
                rounded-2xl
                text-xl
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />
          </div>

          <div
            className="
              flex
              flex-wrap
              gap-2
              mb-2
            "
          >
            <button
              onClick={openCreateGroup}
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
              <Plus size={23} />
              Create Group
            </button>

            <button
              onClick={() => setShowManageGroupModal(true)}
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
              <Users size={23} />
              Manage Group
            </button>
          </div>

          <p
            className="
              text-2xl
              mb-3
            "
          >
            Total Students:{" "}
            <span className="font-medium">{students.length}</span>
          </p>

          <div>
            {filteredStudents.length === 0 && (
              <div
                className="
                  py-10
                  text-center
                  text-gray-500
                "
              >
                {studentSearch
                  ? "No students match your search."
                  : "No students are enrolled in this course."}
              </div>
            )}

            {filteredStudents.map((student, index) => (
              <div
                key={student.id}
                className="
                    flex
                    items-center
                    justify-between
                    border-b
                    border-gray-300
                    py-4
                    px-2
                  "
              >
                <div
                  className="
                      flex
                      items-center
                      gap-5
                    "
                >
                  {student.imageUrl ? (
                    <img
                      src={student.imageUrl}
                      alt={`${student.firstName || ""} ${
                        student.lastName || ""
                      }`}
                      className="
                          w-14
                          h-14
                          rounded-full
                          object-cover
                        "
                    />
                  ) : (
                    <div
                      className="
                          w-14
                          h-14
                          rounded-full
                          bg-gray-300
                          flex
                          items-center
                          justify-center
                        "
                    >
                      <User size={30} className="text-gray-600" />
                    </div>
                  )}
                  <div>
                    <p
                      className="
                          text-xl
                          font-medium
                        "
                    >
                      {student.firstName || ""} {student.lastName || ""}
                    </p>

                    {student.email && (
                      <p
                        className="
                            text-sm
                            text-gray-500
                          "
                      >
                        {student.email}
                      </p>
                    )}
                  </div>
                </div>
                <span
                  className="
                      border
                      border-gray-300
                      rounded-full
                      px-4
                      py-2
                      text-sm
                      whitespace-nowrap
                    "
                >
                  {getStudentGroup(student.id)}
                </span>
              </div>
            ))}
          </div>
        </section>
      )}

      {showManageGroupModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white w-[530px] rounded-xl p-5">
            <div className="flex items-center justify-between mb-5">
              <h2 className="text-3xl font-medium">Groups</h2>

              <button
                onClick={() => setShowManageGroupModal(false)}
                className="text-3xl hover:text-gray-600"
              >
                ×
              </button>
            </div>

            <div className="space-y-3">
              {groups.map((group) => (
                <div
                  key={group.id}
                  className="
                    border
                    border-gray-300
                    rounded-xl
                    p-4
                  "
                >
                  <div className="flex justify-between items-center">
                    <h3 className="text-xl">{group.groupName}</h3>

                    <div className="flex gap-5">
                      <button
                        onClick={() => openEditGroup(group)}
                        className="
                          text-2xl
                          hover:text-indigo-700
                        "
                      >
                        ✎
                      </button>

                      <button
                        onClick={() => handleDeleteGroup(group.id)}
                        className="
                          text-red-600
                          text-2xl
                          hover:text-red-800
                        "
                      >
                        🗑
                      </button>
                    </div>
                  </div>

                  <div className="flex gap-8 mt-4 text-gray-600">
                    <span>Members: {group.studentIds?.length || 0}</span>

                    <span>
                      Last modified:{" "}
                      {group.updatedAt ? formatDate(group.updatedAt) : "—"}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {showCreateGroupModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white w-[500px] max-h-[90vh] rounded-xl p-5">
            <h2 className="text-2xl font-medium mb-4">Group Name</h2>

            <input
              type="text"
              value={groupName}
              onChange={(e) => setGroupName(e.target.value)}
              placeholder="Group 1"
              className="
                w-full
                px-4
                py-3
                bg-gray-200
                rounded-xl
                border
                border-gray-300
                outline-none
                mb-5
              "
            />

            <h2 className="text-2xl font-medium mb-3">Select students</h2>

            <div className="max-h-[550px] overflow-y-auto">
              {students.map((student) => (
                <label
                  key={student.id}
                  className="
                    flex
                    items-center
                    gap-4
                    px-2
                    py-4
                    border-b
                    border-gray-300
                    cursor-pointer
                  "
                >
                  <input
                    type="checkbox"
                    checked={selectedStudents.includes(student.id)}
                    onChange={() => handleStudentSelection(student.id)}
                    className="w-5 h-5"
                  />

                  <div
                    className="
                    w-14
                    h-14
                    rounded-full
                    bg-gray-300
                    flex
                    items-center
                    justify-center
                    text-gray-600
                    text-2xl
                  "
                  >
                    ○
                  </div>

                  <span className="text-lg">
                    {student.firstName} {student.lastName}
                  </span>
                </label>
              ))}
            </div>

            <div className="flex gap-3 mt-5">
              <button
                onClick={handleCreateGroup}
                className="
                  px-5
                  py-3
                  bg-indigo-800
                  hover:bg-indigo-700
                  text-white
                  rounded-lg
                  text-lg
                "
              >
                + Create Group
              </button>

              <button
                onClick={() => {
                  setGroupName("");
                  setSelectedStudents([]);
                  setShowCreateGroupModal(false);
                }}
                className="
                  px-5
                  py-3
                  border
                  border-gray-300
                  rounded-lg
                  text-lg
                "
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {showEditGroupModal && editingGroup && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white w-[500px] max-h-[90vh] rounded-xl p-5">
            <h2 className="text-2xl font-medium mb-4">Group Name</h2>

            <input
              type="text"
              value={groupName}
              onChange={(e) => setGroupName(e.target.value)}
              className="
                w-full
                px-4
                py-3
                bg-gray-200
                rounded-xl
                border
                border-gray-300
                outline-none
                mb-5
              "
            />

            <h2 className="text-2xl font-medium mb-3">Students</h2>

            <div className="max-h-[550px] overflow-y-auto">
              {students.map((student) => (
                <label
                  key={student.id}
                  className="
                    flex
                    items-center
                    gap-4
                    px-2
                    py-4
                    border-b
                    border-gray-300
                    cursor-pointer
                  "
                >
                  <input
                    type="checkbox"
                    checked={selectedStudents.includes(student.id)}
                    onChange={() => handleStudentSelection(student.id)}
                    className="w-5 h-5"
                  />

                  <div
                    className="
                    w-14
                    h-14
                    rounded-full
                    bg-gray-300
                    flex
                    items-center
                    justify-center
                    text-gray-600
                    text-2xl
                  "
                  >
                    ○
                  </div>

                  <span className="text-lg">
                    {student.firstName} {student.lastName}
                  </span>
                </label>
              ))}
            </div>

            <div className="flex gap-3 mt-5">
              <button
                onClick={handleEditGroup}
                className="
                  px-5
                  py-3
                  bg-indigo-800
                  hover:bg-indigo-700
                  text-white
                  rounded-lg
                  text-lg
                "
              >
                + Save Changes
              </button>

              <button
                onClick={() => {
                  setEditingGroup(null);
                  setGroupName("");
                  setSelectedStudents([]);
                  setShowEditGroupModal(false);
                }}
                className="
                  px-5
                  py-3
                  border
                  border-gray-300
                  rounded-lg
                  text-lg
                "
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
