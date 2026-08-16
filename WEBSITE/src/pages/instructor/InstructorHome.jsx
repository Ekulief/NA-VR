import { useEffect, useState } from "react";
import { db, storage } from "../../config/firebase-config";
import { useNavigate } from "react-router-dom";

import {
  doc,
  getDoc,
  collection,
  getDocs,
  query,
  where,
  addDoc,
  serverTimestamp,
} from "firebase/firestore";

import { ref, uploadBytes, getDownloadURL } from "firebase/storage";

import { useAuth } from "../../context/AuthContext";

export default function InstructorHome() {
  const navigate = useNavigate();

  const { user } = useAuth();

  const [courses, setCourses] = useState([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [showCreateCourse, setShowCreateCourse] = useState(false);

  const [courseName, setCourseName] = useState("");
  const [classCode, setClassCode] = useState("");
  const [schedule, setSchedule] = useState("");
  const [courseImage, setCourseImage] = useState(null);

  const [creatingCourse, setCreatingCourse] = useState(false);

  useEffect(() => {
    const getCourses = async () => {
      if (!user) {
        setLoading(false);
        return;
      }

      try {
        const userRef = doc(db, "user", user.uid);

        const userSnapshot = await getDoc(userRef);

        if (!userSnapshot.exists()) {
          throw new Error("User document does not exist.");
        }

        const userData = userSnapshot.data();
        const blocksRef = collection(db, "block");
        const q = query(blocksRef, where("instructorId", "==", user.uid));
        const querySnapshot = await getDocs(q);

        const courseList = querySnapshot.docs.map((blockDoc) => ({
          id: blockDoc.id,
          ...blockDoc.data(),
        }));
        setCourses(courseList);
      } catch (error) {
        console.error("Error getting courses:", error);
        setError("Unable to load your courses.");
      } finally {
        setLoading(false);
      }
    };
    getCourses();
  }, [user]);

  const handleCreateCourse = async (e) => {
    e.preventDefault();

    if (!user) {
      setError("You must be logged in.");
      return;
    }

    if (!courseImage) {
      setError("Please select a course image.");
      return;
    }

    setCreatingCourse(true);
    setError("");

    try {
      const userRef = doc(db, "user", user.uid);

      const userSnapshot = await getDoc(userRef);

      if (!userSnapshot.exists()) {
        throw new Error("Instructor account not found.");
      }

      const userData = userSnapshot.data();

      const imageRef = ref(
        storage,
        `course-images/${Date.now()}-${courseImage.name}`,
      );

      await uploadBytes(imageRef, courseImage);

      const imageUrl = await getDownloadURL(imageRef);

      const newCourse = {
        className: courseName,
        classCode: classCode,
        instructorId: userData._id,
        schedule: schedule,
        academicYear: "2026-2027",
        studentIds: [],
        imageUrl: imageUrl,
        updatedAt: serverTimestamp(),
      };

      const courseRef = await addDoc(collection(db, "block"), newCourse);

      setCourses((previousCourses) => [
        ...previousCourses,
        {
          id: courseRef.id,
          ...newCourse,
        },
      ]);

      setCourseName("");
      setCourseNumber("");
      setClassCode("");
      setSchedule("");
      setCourseImage(null);

      setShowCreateCourse(false);
    } catch (error) {
      console.error("Error creating course:", error);

      setError("Unable to create course. Please try again.");
    } finally {
      setCreatingCourse(false);
    }
  };

  const closeModal = () => {
    if (creatingCourse) return;

    setShowCreateCourse(false);

    setCourseName("");
    setClassCode("");
    setSchedule("");
    setCourseImage(null);
  };

  return (
    <div className="font-google min-h-screen text-black">
      <section className="px-3 py-23">
        <div className="flex items-center justify-between mb-5">
          <h1 className="text-2xl font-medium">My courses</h1>

          <button
            onClick={() => setShowCreateCourse(true)}
            className="
              px-4 py-2
              flex items-center justify-center
              gap-2
              bg-indigo-800
              hover:bg-indigo-700
              text-white
              rounded-lg
              transition
            "
          >
            <span className="text-lg">+</span>
            Create Course
          </button>
        </div>
        {error && <p className="text-red-500 mb-4">{error}</p>}
        {loading && <p className="text-gray-500">Loading courses...</p>}
        {!loading && !error && courses.length === 0 && (
          <p className="text-gray-500">You have not created any course yet.</p>
        )}
        <div className="flex flex-wrap gap-4">
          {courses.map((course) => (
            <div
              onClick={() => navigate(`/instructor/course/${course.id}`)}
              key={course.id}
              className="
                border border-gray-300
                w-80
                rounded-lg
                overflow-hidden
                cursor-pointer
                hover:shadow-md
                transition
              "
            >
              <img
                src={course.imageUrl}
                alt={course.className}
                className="w-full h-40 object-cover"
              />

              <div className="px-4 py-4">
                <h2 className="text-xl font-semibold">{course.classCode}</h2>

                <h3 className="text-lg">{course.className}</h3>

                <p className="text-gray-600">{course.schedule}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {showCreateCourse && (
        <div
          className="
            fixed inset-0
            z-50
            flex items-center justify-center
            bg-black/50
            px-4
          "
          onClick={closeModal}
        >
          <div
            className="
              bg-white
              w-full
              max-w-2xl
              rounded-xl
              shadow-2xl
              p-6
              max-h-[90vh]
              overflow-y-auto
            "
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-5">
              <h2 className="text-2xl font-semibold">New Course</h2>

              <button
                type="button"
                onClick={closeModal}
                disabled={creatingCourse}
                className="
                  text-gray-500
                  hover:text-black
                  text-2xl
                  transition
                "
              >
                ×
              </button>
            </div>
            <form
              onSubmit={handleCreateCourse}
              className="
                border
                border-gray-300
                rounded-lg
                p-4
              "
            >
              <h3 className="text-lg font-medium mb-4">Basic Information</h3>
              <div className="mb-4">
                <label
                  htmlFor="courseName"
                  className="
                    block
                    text-sm
                    font-medium
                    mb-1
                  "
                >
                  Course Name
                </label>

                <input
                  id="courseName"
                  type="text"
                  required
                  value={courseName}
                  onChange={(e) => setCourseName(e.target.value)}
                  placeholder="e.g., PSYCH221 - Experimental Psychology"
                  className="
                    w-full
                    px-3 py-2
                    bg-gray-100
                    border
                    border-gray-300
                    rounded-lg
                    outline-none
                    focus:ring-2
                    focus:ring-indigo-500
                  "
                />
              </div>
              <div className="mb-4">
                <label
                  htmlFor="classCode"
                  className="
                    block
                    text-sm
                    font-medium
                    mb-1
                  "
                >
                  Class Code
                </label>

                <input
                  id="classCode"
                  type="text"
                  required
                  value={classCode}
                  onChange={(e) => setClassCode(e.target.value)}
                  placeholder="e.g., 9472"
                  className="
                    w-full
                    px-3 py-2
                    bg-gray-100
                    border
                    border-gray-300
                    rounded-lg
                    outline-none
                    focus:ring-2
                    focus:ring-indigo-500
                  "
                />
              </div>
              <div className="mb-4">
                <label
                  htmlFor="schedule"
                  className="
                    block
                    text-sm
                    font-medium
                    mb-1
                  "
                >
                  Class Schedule
                </label>

                <input
                  id="schedule"
                  type="text"
                  required
                  value={schedule}
                  onChange={(e) => setSchedule(e.target.value)}
                  placeholder="e.g., 1:30 - 2:30 MTh"
                  className="
                    w-full
                    px-3 py-2
                    bg-gray-100
                    border
                    border-gray-300
                    rounded-lg
                    outline-none
                    focus:ring-2
                    focus:ring-indigo-500
                  "
                />
              </div>
              <div className="mb-4">
                <label
                  htmlFor="courseImage"
                  className="
                    block
                    text-sm
                    font-medium
                    mb-1
                  "
                >
                  Course Image
                </label>

                <input
                  id="courseImage"
                  type="file"
                  required
                  accept="image/*"
                  onChange={(e) => setCourseImage(e.target.files[0])}
                  className="
                    w-full
                    px-3 py-2
                    bg-gray-100
                    border
                    border-gray-300
                    rounded-lg
                    cursor-pointer
                  "
                />
              </div>
              {error && (
                <div
                  className="
                  bg-red-50
                  border border-red-300
                  text-red-600
                  px-3 py-2
                  rounded-lg
                  mb-4
                  text-sm
                "
                >
                  {error}
                </div>
              )}
              <div
                className="
                flex
                justify-end
                gap-3
                mt-5
              "
              >
                <button
                  type="button"
                  onClick={closeModal}
                  disabled={creatingCourse}
                  className="
                    px-4 py-2
                    border
                    border-gray-300
                    rounded-lg
                    hover:bg-gray-100
                    transition
                  "
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  disabled={creatingCourse}
                  className="
                    px-4 py-2
                    bg-indigo-800
                    hover:bg-indigo-700
                    disabled:bg-indigo-400
                    text-white
                    rounded-lg
                    transition
                  "
                >
                  {creatingCourse ? "Creating..." : "Create Course"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
