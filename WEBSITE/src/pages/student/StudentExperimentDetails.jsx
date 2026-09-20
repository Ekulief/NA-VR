import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { db } from "../../config/firebase-config";
import { auth } from "../../config/firebase-config";

import {
  doc,
  getDoc,
  collection,
  getDocs,
  query,
  where,
  deleteDoc,
} from "firebase/firestore";

import { Play, Plus, ArrowLeft, Pencil, Trash2 } from "lucide-react";

export default function StudentExperimentDetails() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();
  const [course, setCourse] = useState(null);
  const [experiment, setExperiment] = useState(null);
  const [studentExperiment, setStudentExperiment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const getDetails = async () => {
      try {
        setLoading(true);
        setError("");

        const courseRef = doc(db, "block", blockId);
        const courseSnap = await getDoc(courseRef);

        if (!courseSnap.exists()) {
          setError("Course not found.");
          return;
        }

        const courseData = {
          id: courseSnap.id,
          ...courseSnap.data(),
        };

        setCourse(courseData);

        if (experimentId) {
          const experimentRef = doc(db, "experiment", experimentId);

          const experimentSnap = await getDoc(experimentRef);

          if (experimentSnap.exists()) {
            setExperiment({
              id: experimentSnap.id,
              ...experimentSnap.data(),
            });
          }
        } else {
          const experimentsRef = collection(db, "experiment");

          const q = query(experimentsRef, where("blockId", "==", blockId));

          const experimentSnapshot = await getDocs(q);
        }

        const currentUser = auth.currentUser;

        if (currentUser) {
          const experimentsRef = collection(db, "experiment");

          const studentQuery = query(
            experimentsRef,
            where("blockId", "==", blockId),
          );

          const studentSnapshot = await getDocs(studentQuery);

          const existingStudentExperiment = studentSnapshot.docs.find(
            (experimentDoc) => {
              const data = experimentDoc.data();

              return data.createdByStudent === currentUser.uid;
            },
          );

          if (existingStudentExperiment) {
            setStudentExperiment({
              id: existingStudentExperiment.id,
              ...existingStudentExperiment.data(),
            });
          } else {
            setStudentExperiment(null);
          }
        }
      } catch (error) {
        console.error("Error getting experiment details:", error);

        setError("Unable to load the experiment details.");
      } finally {
        setLoading(false);
      }
    };

    if (blockId) {
      getDetails();
    }
  }, [blockId, experimentId]);

  if (loading) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p>Loading...</p>
      </div>
    );
  }

  if (error || !course) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p className="text-gray-500">{error || "Course not found."}</p>
      </div>
    );
  }

  const formatDate = (date) => {
    if (!date) {
      return "No date";
    }

    try {
      if (date.toDate) {
        return date.toDate().toLocaleDateString("en-US", {
          month: "short",
          day: "numeric",
          year: "numeric",
        });
      }

      return new Date(date).toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
      });
    } catch {
      return "No date";
    }
  };

  const handleBack = () => {
    navigate(`/student/course/${blockId}`);
  };

  const handleCreateExperiment = () => {
    if (studentExperiment) {
      return;
    }

    navigate(`/student/course/${blockId}/experiment/create`);
  };

  const handleEditExperiment = () => {
    if (!studentExperiment?.id) {
      return;
    }

    navigate(
      `/student/course/${blockId}/experiment/${studentExperiment.id}/edit`,
    );
  };

  const handleRunExperiment = () => {
    if (!experiment?.id) {
      return;
    }

    navigate(`/student/course/${blockId}/experiment/${experiment.id}/run`);
  };

  const handleDeleteExperiment = async () => {
    if (!studentExperiment?.id) {
      return;
    }

    const confirmed = window.confirm(
      "Are you sure you want to delete your experiment? This action cannot be undone.",
    );

    if (!confirmed) {
      return;
    }

    try {
      setDeleting(true);

      const experimentRef = doc(db, "experiment", studentExperiment.id);

      await deleteDoc(experimentRef);

      setStudentExperiment(null);

      alert("Your experiment has been deleted.");
    } catch (error) {
      console.error("Error deleting student experiment:", error);

      alert("Unable to delete your experiment. Please try again.");
    } finally {
      setDeleting(false);
    }
  };

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20 px-5 pb-10">
        <button
          onClick={handleBack}
          className="
            flex
            items-center
            gap-2
            text-gray-600
            hover:text-black
            transition
            mb-4
          "
        >
          <ArrowLeft size={20} />

          <span className="text-lg">Back to Course</span>
        </button>

        <h1 className="text-3xl font-medium mb-4">
          {course.name || course.courseName || course.title || "Final Project"}
        </h1>

        {course.instructions && (
          <div className="mb-6">
            <div
              className="
                text-lg
                text-gray-700
                whitespace-pre-line
                leading-relaxed
              "
            >
              {course.instructions}
            </div>
          </div>
        )}

        {course.scoringCriteria && (
          <section className="mb-8">
            <h2 className="text-2xl font-medium mb-3">Scoring Criteria</h2>

            <div
              className="
                text-lg
                text-gray-600
                whitespace-pre-line
                leading-relaxed
              "
            >
              {course.scoringCriteria}
            </div>
          </section>
        )}

        <section
          className="
            border
            border-gray-300
            rounded-xl
            p-5
            mt-5
          "
        >
          <h2 className="text-2xl font-medium mb-4">Experiment</h2>

          {!experiment ? (
            <p className="text-gray-500">
              No experiment has been created for this activity yet.
            </p>
          ) : (
            <>
              <div className="mb-6">
                <h3 className="text-2xl font-medium mb-3">
                  {experiment.experimentName || "Untitled Experiment"}
                </h3>

                {experiment.instructions && (
                  <div className="mb-5">
                    <p
                      className="
                        text-lg
                        text-gray-600
                        whitespace-pre-line
                        leading-relaxed
                      "
                    >
                      {experiment.instructions}
                    </p>
                  </div>
                )}

                {experiment.participantInstructions && (
                  <div className="mb-5">
                    <h3 className="text-xl font-medium mb-2">
                      Participant Instructions
                    </h3>

                    <p
                      className="
                        text-lg
                        text-gray-600
                        whitespace-pre-line
                        leading-relaxed
                      "
                    >
                      {experiment.participantInstructions}
                    </p>
                  </div>
                )}

                {experiment.scoringCriteria && (
                  <div className="mb-5">
                    <h3 className="text-xl font-medium mb-2">
                      Scoring Criteria
                    </h3>

                    <p
                      className="
                        text-lg
                        text-gray-600
                        whitespace-pre-line
                        leading-relaxed
                      "
                    >
                      {experiment.scoringCriteria}
                    </p>
                  </div>
                )}

                <div className="flex flex-wrap gap-8 text-gray-600">
                  {experiment.vrEnvironment && (
                    <p>
                      <span className="font-medium">Environment:</span>{" "}
                      {experiment.vrEnvironment}
                    </p>
                  )}

                  {experiment.stimuli && (
                    <p>
                      <span className="font-medium">Stimuli:</span>{" "}
                      {experiment.stimuli.length}
                    </p>
                  )}

                  {experiment.duration && (
                    <p>
                      <span className="font-medium">Duration:</span>{" "}
                      {experiment.duration}
                    </p>
                  )}

                  {experiment.dueAt && (
                    <p>
                      <span className="font-medium">Due:</span>{" "}
                      {formatDate(experiment.dueAt)}
                    </p>
                  )}
                </div>
              </div>

              <div className="flex items-center gap-3">
                <button
                  onClick={handleRunExperiment}
                  className="
                    flex
                    items-center
                    gap-2
                    bg-indigo-800
                    hover:bg-indigo-700
                    text-white
                    px-5
                    py-3
                    rounded-lg
                    text-lg
                    transition
                  "
                >
                  <Play size={22} fill="currentColor" />

                  <span>Run Experiment</span>
                </button>

                {experiment.allowStudentExperiments === true && (
                  <button
                    onClick={handleCreateExperiment}
                    disabled={!!studentExperiment}
                    className="
                      flex
                      items-center
                      gap-2
                      border
                      border-indigo-800
                      text-indigo-800
                      hover:bg-indigo-50
                      disabled:border-gray-300
                      disabled:text-gray-400
                      disabled:bg-gray-100
                      disabled:cursor-not-allowed
                      px-5
                      py-3
                      rounded-lg
                      text-lg
                      transition
                    "
                  >
                    <Plus size={22} />

                    <span>
                      {studentExperiment
                        ? "Experiment Created"
                        : "Create Experiment"}
                    </span>
                  </button>
                )}
              </div>
            </>
          )}
        </section>

        {studentExperiment && (
          <section
            className="
              border
              border-gray-300
              rounded-xl
              p-5
              mt-6
            "
          >
            <div className="flex items-center justify-between mb-5">
              <div>
                <h2 className="text-2xl font-medium">My Experiment</h2>

                <p className="text-gray-500 mt-1">
                  Your experiment for this activity
                </p>
              </div>
            </div>

            <div className="mb-5">
              <h3 className="text-2xl font-medium mb-3">
                {studentExperiment.experimentName || "Untitled Experiment"}
              </h3>
            </div>

            {studentExperiment.instructions && (
              <div className="mb-5">
                <h3 className="text-xl font-medium mb-2">Instructions</h3>

                <p
                  className="
                    text-lg
                    text-gray-600
                    whitespace-pre-line
                    leading-relaxed
                  "
                >
                  {studentExperiment.instructions}
                </p>
              </div>
            )}

            {studentExperiment.participantInstructions && (
              <div className="mb-5">
                <h3 className="text-xl font-medium mb-2">
                  Participant Instructions
                </h3>

                <p
                  className="
                    text-lg
                    text-gray-600
                    whitespace-pre-line
                    leading-relaxed
                  "
                >
                  {studentExperiment.participantInstructions}
                </p>
              </div>
            )}

            <div className="flex flex-wrap gap-8 text-gray-600 mb-6">
              {(studentExperiment.environment ||
                studentExperiment.vrEnvironment) && (
                <p>
                  <span className="font-medium">Environment:</span>{" "}
                  {studentExperiment.environment ||
                    studentExperiment.vrEnvironment}
                </p>
              )}

              {studentExperiment.stimuli && (
                <p>
                  <span className="font-medium">Stimuli:</span>{" "}
                  {studentExperiment.stimuli.length}
                </p>
              )}

              {studentExperiment.duration && (
                <p>
                  <span className="font-medium">Duration:</span>{" "}
                  {studentExperiment.duration}
                </p>
              )}

              {studentExperiment.createdAt && (
                <p>
                  <span className="font-medium">Created:</span>{" "}
                  {formatDate(studentExperiment.createdAt)}
                </p>
              )}
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <button
                onClick={handleEditExperiment}
                className="
                  flex
                  items-center
                  gap-2
                  bg-indigo-800
                  hover:bg-indigo-700
                  text-white
                  px-5
                  py-3
                  rounded-lg
                  text-lg
                  transition
                "
              >
                <Pencil size={20} />

                <span>Edit Experiment</span>
              </button>

              <button
                onClick={handleDeleteExperiment}
                disabled={deleting}
                className="
                  flex
                  items-center
                  gap-2
                  border
                  border-red-600
                  text-red-600
                  hover:bg-red-50
                  disabled:border-gray-300
                  disabled:text-gray-400
                  disabled:cursor-not-allowed
                  px-5
                  py-3
                  rounded-lg
                  text-lg
                  transition
                "
              >
                <Trash2 size={20} />

                <span>{deleting ? "Deleting..." : "Delete Experiment"}</span>
              </button>
            </div>
          </section>
        )}
      </main>
    </div>
  );
}
