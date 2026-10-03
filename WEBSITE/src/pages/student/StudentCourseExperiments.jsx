import { useEffect, useMemo, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";

import { db } from "../../config/firebase-config";

import {
  doc,
  getDoc,
  collection,
  getDocs,
  query,
  where,
} from "firebase/firestore";

import { FileText, User } from "lucide-react";

export default function StudentCourseExperiments() {
  const navigate = useNavigate();
  const { blockId } = useParams();

  const [course, setCourse] = useState(null);
  const [loading, setLoading] = useState(true);

  const [experiments, setExperiments] = useState([]);
  const [students, setStudents] = useState([]);

  const [activeTab, setActiveTab] = useState("experiments");
  const [searchTerm, setSearchTerm] = useState("");

  useEffect(() => {
    const getCourseData = async () => {
      try {
        setLoading(true);

        const courseRef = doc(db, "block", blockId);
        const courseSnap = await getDoc(courseRef);

        if (!courseSnap.exists()) {
          console.log("Course doesn't exist.");
          setCourse(null);
          return;
        }

        const courseData = {
          id: courseSnap.id,
          ...courseSnap.data(),
        };

        setCourse(courseData);

        const experimentsRef = collection(db, "experiment");

        const experimentQuery = query(
          experimentsRef,
          where("blockId", "==", blockId),
        );

        const experimentSnapshot = await getDocs(experimentQuery);

        const experimentsList = experimentSnapshot.docs.map(
          (experimentDoc) => ({
            id: experimentDoc.id,
            ...experimentDoc.data(),
          }),
        );

        setExperiments(experimentsList);

        const studentIds = courseData.studentIds || [];

        if (studentIds.length === 0) {
          setStudents([]);
          return;
        }

        const groupsRef = collection(db, "group");

        const groupQuery = query(groupsRef, where("blockId", "==", blockId));

        const groupSnapshot = await getDocs(groupQuery);

        const studentGroupMap = {};

        groupSnapshot.docs.forEach((groupDoc) => {
          const groupData = groupDoc.data();

          const groupName = groupData.groupName || groupData.name || `Group`;

          const groupStudentIds = groupData.studentIds || [];

          groupStudentIds.forEach((studentId) => {
            studentGroupMap[studentId] = groupName;
          });
        });

        const studentPromises = studentIds.map(async (studentId) => {
          const studentRef = doc(db, "user", studentId);

          const studentSnapshot = await getDoc(studentRef);

          if (!studentSnapshot.exists()) {
            return null;
          }

          const studentData = studentSnapshot.data();

          return {
            id: studentSnapshot.id,
            ...studentData,
            groupName: studentGroupMap[studentSnapshot.id] || "Unassigned",
          };
        });

        const studentResults = await Promise.all(studentPromises);

        const validStudents = studentResults.filter(
          (student) => student !== null,
        );

        validStudents.sort((a, b) => {
          const nameA = `${a.firstName || ""} ${a.lastName || ""}`.trim();

          const nameB = `${b.firstName || ""} ${b.lastName || ""}`.trim();

          return nameA.localeCompare(nameB);
        });

        setStudents(validStudents);
      } catch (error) {
        console.error("Error getting course data:", error);
      } finally {
        setLoading(false);
      }
    };

    getCourseData();
  }, [blockId]);

  const filteredStudents = useMemo(() => {
    const search = searchTerm.toLowerCase().trim();

    if (!search) {
      return students;
    }

    return students.filter((student) => {
      const fullName = `${student.firstName || ""} ${
        student.lastName || ""
      }`.toLowerCase();

      const email = (student.email || "").toLowerCase();

      return fullName.includes(search) || email.includes(search);
    });
  }, [students, searchTerm]);

  if (loading) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p>Loading course...</p>
      </div>
    );
  }

  if (!course) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p>Course not found.</p>
      </div>
    );
  }

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20">
        <button
          onClick={() => navigate("/student")}
          className="
            flex
            items-center
            gap-1
            px-4
            pt-4
            pb-2
            text-gray-600
            hover:text-black
            transition
          "
        >
          <span className="text-lg">←</span>
          <span>Back to Home</span>
        </button>

        <div className="border-b border-gray-300">
          <div className="flex items-center gap-8 px-4">
            <button
              onClick={() => setActiveTab("experiments")}
              className={`
                text-xl
                flex
                items-center
                gap-2
                py-3
                transition
                ${
                  activeTab === "experiments"
                    ? "border-b-2 border-black text-black font-medium"
                    : "text-gray-600 hover:text-black"
                }
              `}
            >
              <FileText size={24} />
              <span>Experiments</span>
            </button>

            <button
              onClick={() => setActiveTab("students")}
              className={`
                text-xl
                flex
                items-center
                gap-2
                py-3
                transition
                ${
                  activeTab === "students"
                    ? "border-b-2 border-black text-black font-medium"
                    : "text-gray-600 hover:text-black"
                }
              `}
            >
              <User size={23} />
              <span>Students</span>
            </button>
          </div>
        </div>

        {activeTab === "experiments" && (
          <section className="px-3 py-2">
            {experiments.length === 0 ? (
              <div className="py-10 text-center">
                <p className="text-gray-500">
                  No experiments found for this course.
                </p>
              </div>
            ) : (
              <div className="flex flex-col gap-2">
                {experiments.map((experiment) => (
                  <div
                    key={experiment.id}
                    className="
                      border
                      border-gray-300
                      rounded-xl
                      px-3
                      py-3
                      flex
                      items-center
                      justify-between
                      hover:bg-gray-50
                      transition
                    "
                  >
                    <div className="flex-1">
                      <div className="flex items-center gap-3">
                        <h2 className="font-medium text-xl">
                          {experiment.experimentName || "Untitled Experiment"}
                        </h2>

                        <span
                          className={`
                            px-2
                            py-0.5
                            rounded-full
                            text-md
                            ${
                              experiment.status === "Completed"
                                ? "bg-green-100 text-green-700"
                                : "bg-indigo-100 text-indigo-700"
                            }
                          `}
                        >
                          {experiment.status || "Available"}
                        </span>
                      </div>

                      <div className="flex items-center gap-8 mt-3">
                        <p className="text-md text-gray-600">
                          Due:{" "}
                          {experiment.dueAt
                            ? experiment.dueAt
                                .toDate()
                                .toLocaleDateString("en-US", {
                                  month: "short",
                                  day: "numeric",
                                  year: "numeric",
                                })
                            : "No due date"}
                        </p>

                        <p className="text-md text-gray-600">
                          Total Score:{" "}
                          {experiment.totalScore ?? experiment.maxScore ?? 0}
                        </p>
                      </div>
                    </div>

                    <button
                      onClick={() =>
                        navigate(
                          `/student/course/${blockId}/experiment/${experiment.id}`,
                        )
                      }
                      className="
                        w-8
                        h-8
                        flex
                        items-center
                        justify-center
                        bg-indigo-800
                        hover:bg-indigo-700
                        text-white
                        rounded-lg
                        transition
                      "
                    >
                      <span className="text-lg leading-none">▶</span>
                    </button>
                  </div>
                ))}
              </div>
            )}
          </section>
        )}

        {activeTab === "students" && (
          <section className="max-w-4xl mx-auto px-4 py-5">
            <div
              className="
                w-full
                bg-gray-200
                border
                border-gray-300
                rounded-xl
                px-4
                py-3
                flex
                items-center
                gap-3
                mb-2
              "
            >
              <span className="text-3xl text-gray-600">⌕</span>

              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Search students"
                className="
                  w-full
                  bg-transparent
                  outline-none
                  text-lg
                  text-gray-700
                  placeholder-gray-600
                "
              />
            </div>

            <p className="text-xl mb-4">Total Students: {students.length}</p>

            {filteredStudents.length === 0 ? (
              <div className="py-10 text-center">
                <p className="text-gray-500">
                  {students.length === 0
                    ? "No students are enrolled in this course."
                    : "No students found."}
                </p>
              </div>
            ) : (
              <div>
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
                          <User size={23} />
                        </div>
                      )}

                      <div>
                        <p className="text-xl font-medium">
                          {student.firstName || ""} {student.lastName || ""}
                        </p>
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
                      {student.groupName || "Unassigned"}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </section>
        )}
      </main>
    </div>
  );
}
