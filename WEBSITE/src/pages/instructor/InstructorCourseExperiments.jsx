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

import * as XLSX from "xlsx";

import {
  FileText,
  User,
  Search,
  Play,
  Pencil,
  Trash2,
  Plus,
  ArrowLeft,
  Users,
  UserPlus,
  Upload,
  X,
  Download,
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

  const [groups, setGroups] = useState([]);

  const [showAddStudentModal, setShowAddStudentModal] = useState(false);
  const [addStudentMethod, setAddStudentMethod] = useState("individual");

  const [defaultPassword, setDefaultPassword] = useState("");

  const [newStudent, setNewStudent] = useState({
    firstName: "",
    lastName: "",
    email: "",
    studentNumber: "",
  });

  const [importedStudents, setImportedStudents] = useState([]);
  const [importError, setImportError] = useState("");
  const [addingStudents, setAddingStudents] = useState(false);

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
        setDefaultPassword(courseData.defaultStudentPassword || "");

        const experimentsRef = collection(db, "experiment");

        const experimentQuery = query(
          experimentsRef,
          where("blockId", "==", blockId),
        );

        const experimentSnapshot = await getDocs(experimentQuery);

        const experimentList = experimentSnapshot.docs
          .map((experimentDoc) => ({
            id: experimentDoc.id,
            ...experimentDoc.data(),
          }))
          .filter((experiment) => experiment.createdByStudent !== true);

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

        await getGroups();
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

    const studentNumber = (student.studentNumber || "").toLowerCase();

    return (
      fullName.includes(searchTerm) ||
      email.includes(searchTerm) ||
      studentNumber.includes(searchTerm)
    );
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
      const experimentRef = doc(db, "experiment", experimentId);

      await deleteDoc(experimentRef);

      setExperiments((currentExperiments) =>
        currentExperiments.filter(
          (experiment) => experiment.id !== experimentId,
        ),
      );

      alert("Experiment deleted successfully.");
    } catch (error) {
      console.error("Error deleting experiment:", error);

      alert("Unable to delete the experiment. Please try again.");
    }
  };

  const openAddStudentModal = () => {
    setAddStudentMethod("individual");

    setNewStudent({
      firstName: "",
      lastName: "",
      email: "",
      studentNumber: "",
    });

    setImportedStudents([]);
    setImportError("");

    setShowAddStudentModal(true);
  };

  const closeAddStudentModal = () => {
    if (addingStudents) {
      return;
    }

    setShowAddStudentModal(false);

    setNewStudent({
      firstName: "",
      lastName: "",
      email: "",
      studentNumber: "",
    });

    setImportedStudents([]);
    setImportError("");
  };

  const saveDefaultPassword = async () => {
    if (!blockId) {
      return;
    }

    try {
      const courseRef = doc(db, "block", blockId);

      await updateDoc(courseRef, {
        defaultStudentPassword: defaultPassword,
        updatedAt: serverTimestamp(),
      });

      setCourse((current) => ({
        ...current,
        defaultStudentPassword: defaultPassword,
      }));
    } catch (error) {
      console.error("Error saving default password:", error);

      alert("Unable to save the default password.");
    }
  };

  const handleAddIndividualStudent = async () => {
    const firstName = newStudent.firstName.trim();
    const lastName = newStudent.lastName.trim();
    const email = newStudent.email.trim();
    const studentNumber = newStudent.studentNumber.trim();

    if (!firstName || !lastName) {
      alert("Please enter the student's first and last name.");
      return;
    }

    if (!email) {
      alert("Please enter the student's email.");
      return;
    }

    const duplicateEmail = students.some(
      (student) => student.email?.toLowerCase().trim() === email.toLowerCase(),
    );

    if (duplicateEmail) {
      alert("A student with this email is already enrolled.");
      return;
    }

    if (studentNumber) {
      const duplicateNumber = students.some(
        (student) =>
          student.studentNumber?.toLowerCase().trim() ===
          studentNumber.toLowerCase(),
      );

      if (duplicateNumber) {
        alert("A student with this student number is already enrolled.");
        return;
      }
    }

    try {
      setAddingStudents(true);

      const userRef = await addDoc(collection(db, "user"), {
        firstName,
        lastName,
        email,
        studentNumber,
        role: "student",
        createdAt: serverTimestamp(),
        updatedAt: serverTimestamp(),
      });

      const courseRef = doc(db, "block", blockId);

      const currentStudentIds = course?.studentIds || [];

      await updateDoc(courseRef, {
        studentIds: [...currentStudentIds, userRef.id],
        defaultStudentPassword: defaultPassword,
        updatedAt: serverTimestamp(),
      });

      const createdStudent = {
        id: userRef.id,
        firstName,
        lastName,
        email,
        studentNumber,
        role: "student",
      };

      setStudents((current) => [...current, createdStudent]);

      setCourse((current) => ({
        ...current,
        studentIds: [...currentStudentIds, userRef.id],
        defaultStudentPassword: defaultPassword,
      }));

      alert("Student added successfully.");

      closeAddStudentModal();
    } catch (error) {
      console.error("Error adding student:", error);

      alert("Unable to add the student. Please try again.");
    } finally {
      setAddingStudents(false);
    }
  };

  const normalizeColumnName = (value) => {
    return String(value || "")
      .toLowerCase()
      .replace(/[\s_-]/g, "");
  };

  const findColumn = (row, possibleNames) => {
    const keys = Object.keys(row);

    for (const key of keys) {
      const normalizedKey = normalizeColumnName(key);

      if (
        possibleNames.some(
          (name) => normalizedKey === normalizeColumnName(name),
        )
      ) {
        return row[key];
      }
    }

    return "";
  };

  const processImportedFile = (file) => {
    if (!file) {
      return;
    }

    setImportError("");
    setImportedStudents([]);

    const fileName = file.name.toLowerCase();

    const validFile =
      fileName.endsWith(".csv") ||
      fileName.endsWith(".xls") ||
      fileName.endsWith(".xlsx");

    if (!validFile) {
      setImportError("Please select a CSV, XLS, or XLSX file.");
      return;
    }

    const reader = new FileReader();

    reader.onload = (event) => {
      try {
        const data = new Uint8Array(event.target.result);

        const workbook = XLSX.read(data, {
          type: "array",
        });

        if (!workbook.SheetNames.length) {
          setImportError("The file does not contain any sheets.");
          return;
        }

        const worksheet = workbook.Sheets[workbook.SheetNames[0]];

        const rows = XLSX.utils.sheet_to_json(worksheet, {
          defval: "",
        });

        if (rows.length === 0) {
          setImportError(
            "The selected file does not contain any student records.",
          );
          return;
        }

        const mappedStudents = rows.map((row, index) => ({
          rowNumber: index + 2,

          firstName: String(
            findColumn(row, [
              "firstName",
              "first name",
              "firstname",
              "givenName",
              "given name",
            ]),
          ).trim(),

          lastName: String(
            findColumn(row, [
              "lastName",
              "last name",
              "lastname",
              "surname",
              "familyName",
              "family name",
            ]),
          ).trim(),

          email: String(
            findColumn(row, ["email", "emailAddress", "email address"]),
          ).trim(),

          studentNumber: String(
            findColumn(row, [
              "studentNumber",
              "student number",
              "studentNo",
              "student no",
              "studentId",
              "student id",
              "id",
            ]),
          ).trim(),
        }));

        const nonEmptyStudents = mappedStudents.filter(
          (student) =>
            student.firstName ||
            student.lastName ||
            student.email ||
            student.studentNumber,
        );

        if (nonEmptyStudents.length === 0) {
          setImportError("No student records could be found in the file.");
          return;
        }

        setImportedStudents(nonEmptyStudents);
      } catch (error) {
        console.error("Error reading import file:", error);

        setImportError(
          "Unable to read the file. Please check that it is a valid CSV, XLS, or XLSX file.",
        );
      }
    };

    reader.onerror = () => {
      setImportError("Unable to read the selected file.");
    };

    reader.readAsArrayBuffer(file);
  };

  const removeImportedStudent = (index) => {
    setImportedStudents((current) =>
      current.filter((_, studentIndex) => studentIndex !== index),
    );
  };

  const handleImportStudents = async () => {
    if (importedStudents.length === 0) {
      alert("Please select a file containing students.");
      return;
    }

    const invalidRows = importedStudents.filter(
      (student) => !student.firstName || !student.lastName || !student.email,
    );

    if (invalidRows.length > 0) {
      alert(
        `There are ${invalidRows.length} invalid row(s). Each student must have a first name, last name, and email.`,
      );
      return;
    }

    try {
      setAddingStudents(true);

      const existingEmails = new Set(
        students
          .map((student) => student.email?.toLowerCase().trim())
          .filter(Boolean),
      );

      const existingStudentNumbers = new Set(
        students
          .map((student) => student.studentNumber?.toLowerCase().trim())
          .filter(Boolean),
      );

      const batchEmails = new Set();
      const batchStudentNumbers = new Set();

      const studentsToAdd = [];
      const skippedStudents = [];

      for (const student of importedStudents) {
        const email = student.email.toLowerCase().trim();

        const studentNumber = student.studentNumber.toLowerCase().trim();

        if (existingEmails.has(email)) {
          skippedStudents.push(
            `Row ${student.rowNumber}: ${student.email} already exists`,
          );
          continue;
        }

        if (batchEmails.has(email)) {
          skippedStudents.push(
            `Row ${student.rowNumber}: duplicate email in import`,
          );
          continue;
        }

        if (studentNumber && existingStudentNumbers.has(studentNumber)) {
          skippedStudents.push(
            `Row ${student.rowNumber}: student number ${student.studentNumber} already exists`,
          );
          continue;
        }

        if (studentNumber && batchStudentNumbers.has(studentNumber)) {
          skippedStudents.push(
            `Row ${student.rowNumber}: duplicate student number in import`,
          );
          continue;
        }

        batchEmails.add(email);

        if (studentNumber) {
          batchStudentNumbers.add(studentNumber);
        }

        studentsToAdd.push(student);
      }

      if (studentsToAdd.length === 0) {
        alert(
          "No new students were added. All imported students already exist or are duplicates.",
        );

        setAddingStudents(false);
        return;
      }

      const createdStudents = [];

      for (const student of studentsToAdd) {
        const userRef = await addDoc(collection(db, "user"), {
          firstName: student.firstName,
          lastName: student.lastName,
          email: student.email,
          studentNumber: student.studentNumber,
          role: "student",
          createdAt: serverTimestamp(),
          updatedAt: serverTimestamp(),
        });

        createdStudents.push({
          id: userRef.id,
          firstName: student.firstName,
          lastName: student.lastName,
          email: student.email,
          studentNumber: student.studentNumber,
          role: "student",
        });
      }

      const newStudentIds = createdStudents.map((student) => student.id);

      const currentStudentIds = course?.studentIds || [];

      const courseRef = doc(db, "block", blockId);

      await updateDoc(courseRef, {
        studentIds: [...currentStudentIds, ...newStudentIds],
        defaultStudentPassword: defaultPassword,
        updatedAt: serverTimestamp(),
      });

      setStudents((current) => [...current, ...createdStudents]);

      setCourse((current) => ({
        ...current,
        studentIds: [...currentStudentIds, ...newStudentIds],
        defaultStudentPassword: defaultPassword,
      }));

      let message = `${createdStudents.length} student(s) imported successfully.`;

      if (skippedStudents.length > 0) {
        message += `\n\n${skippedStudents.length} row(s) were skipped:\n\n${skippedStudents
          .slice(0, 10)
          .join("\n")}`;

        if (skippedStudents.length > 10) {
          message += `\n...and ${skippedStudents.length - 10} more.`;
        }
      }

      alert(message);

      closeAddStudentModal();
    } catch (error) {
      console.error("Error importing students:", error);

      alert(
        "An error occurred while importing the students. Some students may have already been added. Please check the student list before trying again.",
      );
    } finally {
      setAddingStudents(false);
    }
  };

  const openCreateGroup = () => {
    setGroupName("");
    setSelectedStudents([]);
    setShowCreateGroupModal(true);
  };

  const openEditGroup = (group) => {
    setEditingGroup(group);

    setGroupName(group.groupName || "");

    setSelectedStudents(group.studentIds || []);

    setShowManageGroupModal(false);
    setShowEditGroupModal(true);
  };

  const getStudentGroup = (studentId) => {
    const group = groups.find((group) => group.studentIds?.includes(studentId));

    return group?.groupName || "Unassigned";
  };

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

  const handleStudentSelection = (studentId) => {
    setSelectedStudents((current) => {
      if (current.includes(studentId)) {
        return current.filter((id) => id !== studentId);
      }

      return [...current, studentId];
    });
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

  return (
    <div
      className="
        px-60
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
        <section className="px-5 py-5">
          <div
            className="
              flex
              items-center
              justify-between
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
          </div>

          <div
            className="
              relative
              my-4
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
            max-w-5xl
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
              mb-5
            "
          >
            <button
              onClick={openAddStudentModal}
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
              <UserPlus size={23} />
              Add Students
            </button>

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

            {filteredStudents.map((student) => (
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

                    {student.studentNumber && (
                      <p
                        className="
                            text-sm
                            text-gray-500
                          "
                      >
                        {student.studentNumber}
                      </p>
                    )}

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

      {showAddStudentModal && (
        <div
          className="
            fixed
            inset-0
            bg-black/50
            flex
            items-center
            justify-center
            z-50
            p-4
          "
        >
          <div
            className="
              bg-white
              w-full
              max-w-3xl
              max-h-[90vh]
              rounded-2xl
              shadow-xl
              overflow-hidden
              flex
              flex-col
            "
          >
            <div
              className="
                flex
                items-center
                justify-between
                px-6
                py-5
                border-b
                border-gray-200
              "
            >
              <div>
                <h2 className="text-3xl font-medium">Add Students</h2>

                <p className="text-gray-500 mt-1">
                  Add students individually or import them from a spreadsheet.
                </p>
              </div>

              <button
                onClick={closeAddStudentModal}
                className="
                  text-gray-500
                  hover:text-black
                  transition
                "
              >
                <X size={28} />
              </button>
            </div>

            <div className="overflow-y-auto px-6 py-5">
              <div
                className="
                  grid
                  grid-cols-2
                  gap-4
                  mb-6
                "
              >
                <button
                  onClick={() => setAddStudentMethod("individual")}
                  className={`
                    border
                    rounded-xl
                    p-5
                    text-left
                    transition
                    ${
                      addStudentMethod === "individual"
                        ? "border-indigo-800 bg-indigo-50"
                        : "border-gray-300 hover:border-gray-500"
                    }
                  `}
                >
                  <UserPlus size={28} className="mb-2" />

                  <p className="text-xl font-medium">Add Individually</p>

                  <p className="text-gray-500 text-sm mt-1">
                    Enter one student's details.
                  </p>
                </button>

                <button
                  onClick={() => setAddStudentMethod("import")}
                  className={`
                    border
                    rounded-xl
                    p-5
                    text-left
                    transition
                    ${
                      addStudentMethod === "import"
                        ? "border-indigo-800 bg-indigo-50"
                        : "border-gray-300 hover:border-gray-500"
                    }
                  `}
                >
                  <Upload size={28} className="mb-2" />

                  <p className="text-xl font-medium">Import File</p>

                  <p className="text-gray-500 text-sm mt-1">
                    Import CSV, XLS, or XLSX.
                  </p>
                </button>
              </div>

              <div
                className="
                  border
                  border-gray-300
                  rounded-xl
                  p-5
                  mb-6
                "
              >
                <h3 className="text-xl font-medium mb-2">
                  Default Student Password
                </h3>

                <p className="text-sm text-gray-500 mb-3">
                  This password is saved with the course and can be used if
                  student accounts are provisioned later.
                </p>

                <div className="flex gap-2">
                  <input
                    type="text"
                    value={defaultPassword}
                    onChange={(e) => setDefaultPassword(e.target.value)}
                    placeholder="Enter default password"
                    className="
                      flex-1
                      px-4
                      py-3
                      bg-gray-100
                      border
                      border-gray-300
                      rounded-xl
                      outline-none
                      focus:ring-2
                      focus:ring-indigo-500
                    "
                  />

                  <button
                    onClick={saveDefaultPassword}
                    className="
                      px-4
                      py-3
                      bg-gray-200
                      hover:bg-gray-300
                      rounded-xl
                      transition
                    "
                  >
                    Save
                  </button>
                </div>
              </div>

              {addStudentMethod === "individual" && (
                <div className="space-y-4">
                  <div
                    className="
                      grid
                      grid-cols-2
                      gap-4
                    "
                  >
                    <div>
                      <label className="block mb-2 font-medium">
                        First Name
                      </label>

                      <input
                        type="text"
                        value={newStudent.firstName}
                        onChange={(e) =>
                          setNewStudent((current) => ({
                            ...current,
                            firstName: e.target.value,
                          }))
                        }
                        className="
                          w-full
                          px-4
                          py-3
                          bg-gray-100
                          border
                          border-gray-300
                          rounded-xl
                          outline-none
                          focus:ring-2
                          focus:ring-indigo-500
                        "
                      />
                    </div>

                    <div>
                      <label className="block mb-2 font-medium">
                        Last Name
                      </label>

                      <input
                        type="text"
                        value={newStudent.lastName}
                        onChange={(e) =>
                          setNewStudent((current) => ({
                            ...current,
                            lastName: e.target.value,
                          }))
                        }
                        className="
                          w-full
                          px-4
                          py-3
                          bg-gray-100
                          border
                          border-gray-300
                          rounded-xl
                          outline-none
                          focus:ring-2
                          focus:ring-indigo-500
                        "
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block mb-2 font-medium">Email</label>

                    <input
                      type="email"
                      value={newStudent.email}
                      onChange={(e) =>
                        setNewStudent((current) => ({
                          ...current,
                          email: e.target.value,
                        }))
                      }
                      className="
                        w-full
                        px-4
                        py-3
                        bg-gray-100
                        border
                        border-gray-300
                        rounded-xl
                        outline-none
                        focus:ring-2
                        focus:ring-indigo-500
                      "
                    />
                  </div>

                  <div>
                    <label className="block mb-2 font-medium">
                      Student Number
                      <span className="text-gray-400 font-normal">
                        {" "}
                        (Optional)
                      </span>
                    </label>

                    <input
                      type="text"
                      value={newStudent.studentNumber}
                      onChange={(e) =>
                        setNewStudent((current) => ({
                          ...current,
                          studentNumber: e.target.value,
                        }))
                      }
                      className="
                        w-full
                        px-4
                        py-3
                        bg-gray-100
                        border
                        border-gray-300
                        rounded-xl
                        outline-none
                        focus:ring-2
                        focus:ring-indigo-500
                      "
                    />
                  </div>
                </div>
              )}

              {addStudentMethod === "import" && (
                <div>
                  <div
                    className="
                      border
                     -2
                      border-dashed
                      border-gray-300
                      rounded-xl
                      p-8
                      text-center
                    "
                  >
                    <Upload
                      size={40}
                      className="
                        mx-auto
                        mb-3
                        text-gray-500
                      "
                    />

                    <h3 className="text-xl font-medium">Select Student File</h3>

                    <p className="text-gray-500 text-sm mt-1 mb-4">
                      Supported formats: CSV, XLS, XLSX
                    </p>

                    <label
                      className="
                        inline-flex
                        items-center
                        gap-2
                        px-5
                        py-3
                        bg-indigo-800
                        hover:bg-indigo-700
                        text-white
                        rounded-lg
                        cursor-pointer
                        transition
                      "
                    >
                      <Upload size={20} />
                      Choose File
                      <input
                        type="file"
                        accept=".csv,.xls,.xlsx"
                        className="hidden"
                        onChange={(e) =>
                          processImportedFile(e.target.files?.[0])
                        }
                      />
                    </label>

                    <p className="text-xs text-gray-500 mt-4">
                      Expected columns: firstName, lastName, email,
                      studentNumber
                    </p>
                  </div>

                  {importError && (
                    <div
                      className="
                        mt-4
                        p-4
                        bg-red-50
                        border
                        border-red-200
                        text-red-700
                        rounded-xl
                      "
                    >
                      {importError}
                    </div>
                  )}

                  {importedStudents.length > 0 && (
                    <div className="mt-5">
                      <div
                        className="
                          flex
                          items-center
                          justify-between
                          mb-3
                        "
                      >
                        <h3 className="text-xl font-medium">Import Preview</h3>

                        <span className="text-gray-500">
                          {importedStudents.length} students
                        </span>
                      </div>

                      <div
                        className="
                          border
                          border-gray-300
                          rounded-xl
                          overflow-hidden
                        "
                      >
                        <div className="max-h-[350px] overflow-y-auto">
                          {importedStudents.map((student, index) => (
                            <div
                              key={`${student.rowNumber}-${index}`}
                              className="
                                  flex
                                  items-center
                                  justify-between
                                  gap-4
                                  px-4
                                  py-3
                                  border-b
                                  border-gray-200
                                  last:border-b-0
                                "
                            >
                              <div className="min-w-0">
                                <p className="font-medium">
                                  {student.firstName} {student.lastName}
                                </p>

                                <p className="text-sm text-gray-500 truncate">
                                  {student.email}
                                </p>

                                {student.studentNumber && (
                                  <p className="text-xs text-gray-400">
                                    {student.studentNumber}
                                  </p>
                                )}
                              </div>

                              <button
                                onClick={() => removeImportedStudent(index)}
                                className="
                                    text-red-500
                                    hover:text-red-700
                                  "
                              >
                                <X size={20} />
                              </button>
                            </div>
                          ))}
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            <div
              className="
                flex
                justify-end
                gap-3
                px-6
                py-4
                border-t
                border-gray-200
              "
            >
              <button
                onClick={closeAddStudentModal}
                disabled={addingStudents}
                className="
                  px-5
                  py-3
                  border
                  border-gray-300
                  rounded-lg
                  hover:bg-gray-100
                  transition
                  disabled:opacity-50
                "
              >
                Cancel
              </button>

              {addStudentMethod === "individual" ? (
                <button
                  onClick={handleAddIndividualStudent}
                  disabled={addingStudents}
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
                    transition
                    disabled:opacity-50
                  "
                >
                  <UserPlus size={20} />

                  {addingStudents ? "Adding..." : "Add Student"}
                </button>
              ) : (
                <button
                  onClick={handleImportStudents}
                  disabled={addingStudents || importedStudents.length === 0}
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
                    transition
                    disabled:opacity-50
                  "
                >
                  <Upload size={20} />

                  {addingStudents
                    ? "Importing..."
                    : `Import ${importedStudents.length || ""} Students`}
                </button>
              )}
            </div>
          </div>
        </div>
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

              {groups.length === 0 && (
                <p className="text-center text-gray-500 py-8">
                  No groups have been created yet.
                </p>
              )}
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
                    <User size={25} />
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
                    <User size={25} />
                  </div>

                  <span className="text-lg">
                    {student.firstName} {student.lastName}
                  </span>
                </label>
              ))}
            </div>

            <div className="flex gap-3 mt-5">
              <button
                onClick={handleSaveGroupChanges}
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
