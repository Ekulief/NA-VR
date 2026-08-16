import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { db } from "../../config/firebase-config";

import {
  doc,
  getDoc,
  collection,
  getDocs,
  query,
  where,
} from "firebase/firestore";

import { Play, Plus, ArrowLeft } from "lucide-react";

export default function StudentExperimentDetails() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();

  const [course, setCourse] = useState(null);
  const [experiment, setExperiment] = useState(null);
  const [studentExperiment, setStudentExperiment] = useState(null);

  const [loading, setLoading] = useState(true);
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

          if (!experimentSnapshot.empty) {
            const experimentDoc = experimentSnapshot.docs[0];

            setExperiment({
              id: experimentDoc.id,
              ...experimentDoc.data(),
            });
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
    navigate(`/student/course/${blockId}/experiment/create`);
  };

  const handleRunExperiment = () => {
    if (!experiment?.id) return;

    navigate(`/student/course/${blockId}/experiment/${experiment.id}/run`);
  };

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20 px-5 pb-10">
        {/* Back button */}
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

        {/* =========================
            COURSE / ACTIVITY DETAILS
            ========================= */}
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

        {/* =========================
            EXPERIMENT SECTION
            ========================= */}
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
              {/* Experiment details */}
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

                {/* Experiment metadata */}
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

              {/* =========================
                  ACTIONS
                  ========================= */}
              <div className="flex items-center gap-3">
                {/* Run instructor experiment */}
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

                {/* Create student experiment
                    ONLY if instructor enabled it */}
                {experiment.allowStudentExperiments === true && (
                  <button
                    onClick={handleCreateExperiment}
                    className="
                      flex
                      items-center
                      gap-2
                      border
                      border-indigo-800
                      text-indigo-800
                      hover:bg-indigo-50
                      px-5
                      py-3
                      rounded-lg
                      text-lg
                      transition
                    "
                  >
                    <Plus size={22} />

                    <span>Create Experiment</span>
                  </button>
                )}
              </div>
            </>
          )}
        </section>
      </main>
    </div>
  );
}
