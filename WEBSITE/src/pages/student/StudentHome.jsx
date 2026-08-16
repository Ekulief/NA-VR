import { useEffect, useState } from "react";
import { db } from "../../config/firebase-config";
import { useNavigate } from "react-router-dom";

import {
  doc,
  getDoc,
  collection,
  getDocs,
  query,
  where,
} from "firebase/firestore";

import { useAuth } from "../../context/AuthContext";

export default function StudentHome() {
  const navigate = useNavigate();

  const { user } = useAuth();

  const [courses, setCourses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const getStudentCourses = async () => {
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

        const blocksRef = collection(db, "block");

        const q = query(
          blocksRef,
          where("studentIds", "array-contains", user.uid),
        );

        const querySnapshot = await getDocs(q);

        const courseList = await Promise.all(
          querySnapshot.docs.map(async (blockDoc) => {
            const courseData = blockDoc.data();

            let instructorName = "Unknown Instructor";

            if (courseData.instructorId) {
              const instructorRef = doc(db, "user", courseData.instructorId);

              const instructorSnapshot = await getDoc(instructorRef);

              if (instructorSnapshot.exists()) {
                const instructorData = instructorSnapshot.data();

                instructorName = `${instructorData.firstName || ""} ${
                  instructorData.lastName || ""
                }`.trim();
              }
            }

            return {
              id: blockDoc.id,
              ...courseData,
              instructorName,
            };
          }),
        );

        setCourses(courseList);
      } catch (error) {
        console.error("Error getting student courses:", error);

        setError("Unable to load your courses.");
      } finally {
        setLoading(false);
      }
    };

    getStudentCourses();
  }, [user]);

  return (
    <div className="font-google min-h-screen text-black">
      <section className="px-3 py-23">
        <h1 className="text-2xl font-medium mb-5">My courses</h1>
        {loading && <p className="text-gray-500">Loading courses...</p>}
        {error && <p className="text-red-500">{error}</p>}
        {!loading && !error && courses.length === 0 && (
          <p className="text-gray-500">You are not enrolled in any courses.</p>
        )}
        <div className="flex flex-wrap gap-4">
          {courses.map((course) => (
            <div
              onClick={() => navigate(`/student/course/${course.id}`)}
              key={course.id}
              className="border border-gray-300 w-80 rounded-lg overflow-hidden cursor-pointer hover:shadow-md transition"
            >
              <img
                src={course.imageUrl}
                alt={course.className}
                className="w-full h-40 object-cover"
              />

              <div className="px-4 py-4">
                <h2 className="text-xl font-semibold">{course.classCode}</h2>

                <h3 className="text-lg">{course.className}</h3>

                <p className="text-gray-600 mt-2">
                  Instructor: {course.instructorName}
                </p>

                <p className="text-gray-600">{course.schedule}</p>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
