import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  doc,
  getDoc,
  addDoc,
  updateDoc,
  collection,
  serverTimestamp,
} from "firebase/firestore";

import { db, auth } from "../../config/firebase-config";

import { ArrowLeft, Plus, X, Image, Box, Upload } from "lucide-react";

export default function StudentExperimentBuilder() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();
  const isEditing = Boolean(experimentId);
  const [experimentName, setExperimentName] = useState("");
  const [instructions, setInstructions] = useState("");
  const [duration, setDuration] = useState("");
  const [environment, setEnvironment] = useState("");
  const [stimuli, setStimuli] = useState([
    {
      id: Date.now(),
      type: "text",
      content: "",
      duration: "",
      positionX: "",
      positionY: "",
      positionZ: "",
      color: "#ff0000",
    },
  ]);
  const [participantInstructions, setParticipantInstructions] = useState("");
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(isEditing);
  const [error, setError] = useState("");
  const environments = [
    {
      name: "Classroom",
      description: "Standard classroom with desks and whiteboard",
    },
    {
      name: "Maze",
      description: "3D maze environment for navigation studies",
    },
    {
      name: "Forest",
      description: "Natural outdoor environment",
    },
    {
      name: "Laboratory",
      description: "Clinical lab setting",
    },
    {
      name: "City Street",
      description: "Urban environment with building and traffic",
    },
  ];

  useEffect(() => {
    const loadExperiment = async () => {
      if (!experimentId) {
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError("");

        const currentUser = auth.currentUser;

        if (!currentUser) {
          setError("You must be logged in to edit an experiment.");
          return;
        }

        const experimentRef = doc(db, "experiment", experimentId);

        const experimentSnap = await getDoc(experimentRef);

        if (!experimentSnap.exists()) {
          setError("Experiment not found.");
          return;
        }

        const data = experimentSnap.data();

        if (data.createdByStudent !== currentUser.uid) {
          setError("You do not have permission to edit this experiment.");
          return;
        }

        if (data.blockId !== blockId) {
          setError("This experiment does not belong to this activity.");
          return;
        }

        setExperimentName(data.experimentName || "");
        setInstructions(data.instructions || "");
        setDuration(data.duration || "");

        setEnvironment(data.environment || data.vrEnvironment || "");

        setParticipantInstructions(data.participantInstructions || "");

        if (Array.isArray(data.stimuli) && data.stimuli.length > 0) {
          setStimuli(
            data.stimuli.map((stimulus, index) => ({
              id: Date.now() + index + Math.random(),
              type: stimulus.type || "text",
              content: stimulus.content || "",
              duration: stimulus.duration ?? "",
              positionX: stimulus.positionX ?? "",
              positionY: stimulus.positionY ?? "",
              positionZ: stimulus.positionZ ?? "",
              color: stimulus.color || "#ff0000",
            })),
          );
        } else {
          setStimuli([
            {
              id: Date.now(),
              type: "text",
              content: "",
              duration: "",
              positionX: "",
              positionY: "",
              positionZ: "",
              color: "#ff0000",
            },
          ]);
        }
      } catch (error) {
        console.error("Error loading student experiment:", error);

        setError("Unable to load the experiment.");
      } finally {
        setLoading(false);
      }
    };

    loadExperiment();
  }, [blockId, experimentId]);

  const addStimulus = (type) => {
    const newStimulus = {
      id: Date.now() + Math.random(),
      type,
      content: "",
      duration: "",
      positionX: "",
      positionY: "",
      positionZ: "",
      color: "#ff0000",
    };

    setStimuli((previous) => [...previous, newStimulus]);
  };

  const removeStimulus = (id) => {
    setStimuli((previous) => previous.filter((stimulus) => stimulus.id !== id));
  };

  const updateStimulus = (id, field, value) => {
    setStimuli((previous) =>
      previous.map((stimulus) =>
        stimulus.id === id
          ? {
              ...stimulus,
              [field]: value,
            }
          : stimulus,
      ),
    );
  };

  const saveExperiment = async () => {
    if (!experimentName.trim()) {
      alert("Please enter an experiment name.");
      return;
    }

    if (!environment) {
      alert("Please select a VR environment.");
      return;
    }

    const currentUser = auth.currentUser;

    if (!currentUser) {
      alert("You must be logged in to create an experiment.");
      return;
    }

    try {
      setSaving(true);

      const formattedStimuli = stimuli.map((stimulus) => ({
        type: stimulus.type,
        content: stimulus.content,
        duration: stimulus.duration,
        positionX: stimulus.positionX,
        positionY: stimulus.positionY,
        positionZ: stimulus.positionZ,

        color: stimulus.type === "text" ? stimulus.color : null,
      }));

      if (isEditing) {
        const experimentRef = doc(db, "experiment", experimentId);

        const experimentSnap = await getDoc(experimentRef);

        if (!experimentSnap.exists()) {
          alert("Experiment not found.");
          return;
        }

        const existingData = experimentSnap.data();

        if (existingData.createdByStudent !== currentUser.uid) {
          alert("You do not have permission to edit this experiment.");
          return;
        }

        await updateDoc(experimentRef, {
          experimentName: experimentName.trim(),
          instructions: instructions.trim(),
          duration: duration,
          environment: environment,
          stimuli: formattedStimuli,
          participantInstructions: participantInstructions.trim(),
          createdByStudent: currentUser.uid,
          updatedAt: serverTimestamp(),
        });

        alert("Experiment updated successfully!");

        navigate(`/student/course/${blockId}/experiment`);

        return;
      }

      const experimentData = {
        experimentName: experimentName.trim(),
        instructions: instructions.trim(),
        duration: duration,
        blockId: blockId,
        environment: environment,
        stimuli: formattedStimuli,
        participantInstructions: participantInstructions.trim(),
        createdByStudent: currentUser.uid,
        createdAt: serverTimestamp(),
        updatedAt: serverTimestamp(),
      };

      await addDoc(collection(db, "experiment"), experimentData);

      alert("Experiment created successfully!");

      navigate(`/student/course/${blockId}/experiment`);
    } catch (error) {
      console.error("Error saving student experiment:", error);

      alert(
        isEditing
          ? "Unable to update the experiment. Please try again."
          : "Unable to create the experiment. Please try again.",
      );
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p>Loading experiment...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="font-google min-h-screen bg-white text-black">
        <main className="pt-20 px-8">
          <button
            onClick={() => navigate(-1)}
            className="
              flex
              items-center
              gap-2
              text-gray-600
              hover:text-black
              transition
              mb-6
            "
          >
            <ArrowLeft size={20} />

            <span>Back</span>
          </button>

          <div className="max-w-5xl mx-auto">
            <p className="text-red-600">{error}</p>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20">
        <button
          onClick={() => navigate(-1)}
          className="
            flex
            items-center
            gap-2
            px-8
            pt-5
            text-gray-600
            hover:text-black
            transition
          "
        >
          <ArrowLeft size={20} />

          <span>Back to Activity</span>
        </button>

        <div className="max-w-5xl mx-auto px-6 pb-12">
          <h1 className="text-3xl font-medium mt-5 mb-6">
            {isEditing ? "Edit Experiment" : "New Experiment"}
          </h1>

          <section
            className="
              border
              border-gray-300
              rounded-lg
              p-4
              mb-5
            "
          >
            <h2 className="text-lg font-medium mb-3">Basic Information</h2>

            <label className="block text-sm mb-1">Experiment Name</label>

            <input
              type="text"
              value={experimentName}
              onChange={(e) => setExperimentName(e.target.value)}
              placeholder="e.g., Stroop Color-Word Test"
              className="
                w-full
                bg-gray-200
                border
                border-gray-300
                rounded-lg
                px-3
                py-2
                mb-2
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />

            <label className="block text-sm mb-1">Instructions</label>

            <textarea
              value={instructions}
              onChange={(e) => setInstructions(e.target.value)}
              placeholder="Describe the tasks needed to perform this experiment"
              rows={4}
              className="
                w-full
                bg-gray-200
                border
                border-gray-300
                rounded-lg
                px-3
                py-2
                resize-none
                mb-2
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />

            <label className="block text-sm mb-1">Duration</label>

            <input
              type="text"
              value={duration}
              onChange={(e) => setDuration(e.target.value)}
              placeholder="e.g., 5 mins"
              className="
                w-full
                bg-gray-200
                border
                border-gray-300
                rounded-lg
                px-3
                py-2
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />
          </section>

          <section
            className="
              border
              border-gray-300
              rounded-lg
              p-4
              mb-5
            "
          >
            <h2 className="text-lg font-medium mb-3">VR Environment</h2>

            <div className="grid grid-cols-2 gap-2">
              {environments.map((env) => (
                <button
                  key={env.name}
                  type="button"
                  onClick={() => setEnvironment(env.name)}
                  className={`
                    text-left
                    border
                    rounded-lg
                    px-3
                    py-2
                    transition
                    ${
                      environment === env.name
                        ? "border-indigo-700 bg-indigo-50"
                        : "border-gray-300 hover:bg-gray-50"
                    }
                  `}
                >
                  <p className="font-medium">{env.name}</p>

                  <p className="text-sm text-gray-500">{env.description}</p>
                </button>
              ))}
            </div>
          </section>

          <section
            className="
              border
              border-gray-300
              rounded-lg
              p-4
              mb-5
            "
          >
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-lg font-medium">
                Stimuli ({stimuli.length})
              </h2>

              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => addStimulus("text")}
                  className="
                    border
                    border-gray-300
                    rounded-lg
                    px-3
                    py-1.5
                    flex
                    items-center
                    gap-1
                    hover:bg-gray-50
                  "
                >
                  <Plus size={15} />
                  Text
                </button>

                <button
                  type="button"
                  onClick={() => addStimulus("image")}
                  className="
                    border
                    border-gray-300
                    rounded-lg
                    px-3
                    py-1.5
                    flex
                    items-center
                    gap-1
                    hover:bg-gray-50
                  "
                >
                  <Image size={15} />
                  Image
                </button>

                <button
                  type="button"
                  onClick={() => addStimulus("3d")}
                  className="
                    border
                    border-gray-300
                    rounded-lg
                    px-3
                    py-1.5
                    flex
                    items-center
                    gap-1
                    hover:bg-gray-50
                  "
                >
                  <Box size={15} />
                  3D Object
                </button>
              </div>
            </div>

            <div className="flex flex-col gap-2">
              {stimuli.map((stimulus) => (
                <div
                  key={stimulus.id}
                  className="
                      border
                      border-gray-300
                      rounded-lg
                      p-3
                    "
                >
                  <div className="flex justify-between mb-2">
                    <h3 className="text-base font-medium">
                      {stimulus.type === "text"
                        ? "Text"
                        : stimulus.type === "image"
                          ? "Image"
                          : "3D Object"}
                    </h3>

                    <button
                      type="button"
                      onClick={() => removeStimulus(stimulus.id)}
                      className="
                          text-red-600
                          hover:text-red-800
                        "
                    >
                      <X size={18} />
                    </button>
                  </div>

                  {stimulus.type === "text" && (
                    <>
                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <label className="block text-sm mb-1">Content</label>

                          <div className="relative">
                            <input
                              type="text"
                              value={stimulus.content}
                              onChange={(e) =>
                                updateStimulus(
                                  stimulus.id,
                                  "content",
                                  e.target.value,
                                )
                              }
                              className="
                                  w-full
                                  bg-gray-200
                                  border
                                  border-gray-300
                                  rounded-lg
                                  px-3
                                  py-2
                                "
                            />

                            <input
                              type="color"
                              value={stimulus.color || "#ff0000"}
                              onChange={(e) =>
                                updateStimulus(
                                  stimulus.id,
                                  "color",
                                  e.target.value,
                                )
                              }
                              className="
                                  absolute
                                  right-2
                                  top-1/2
                                  -translate-y-1/2
                                  w-7
                                  h-7
                                  border-0
                                  bg-transparent
                                  cursor-pointer
                                "
                            />
                          </div>
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Duration (seconds)
                          </label>

                          <input
                            type="number"
                            value={stimulus.duration}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "duration",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>
                      </div>

                      <div className="grid grid-cols-2 gap-2 mt-2">
                        <div>
                          <label className="block text-sm mb-1">
                            Position (X)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionX}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionX",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Position (Y)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionY}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionY",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Position (Z)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionZ}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionZ",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>
                      </div>
                    </>
                  )}

                  {stimulus.type === "image" && (
                    <>
                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <label className="block text-sm mb-1">Content</label>

                          <label
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                                flex
                                items-center
                                justify-between
                                cursor-pointer
                              "
                          >
                            <span className="truncate">
                              {stimulus.content || "Choose image"}
                            </span>

                            <Upload size={18} />

                            <input
                              type="file"
                              accept="image/*"
                              className="hidden"
                              onChange={(e) => {
                                const file = e.target.files?.[0];

                                if (file) {
                                  updateStimulus(
                                    stimulus.id,
                                    "content",
                                    file.name,
                                  );
                                }
                              }}
                            />
                          </label>
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Duration (seconds)
                          </label>

                          <input
                            type="number"
                            value={stimulus.duration}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "duration",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>
                      </div>

                      <div className="grid grid-cols-2 gap-2 mt-2">
                        <div>
                          <label className="block text-sm mb-1">
                            Position (X)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionX}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionX",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Position (Y)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionY}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionY",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Position (Z)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionZ}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionZ",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>
                      </div>
                    </>
                  )}

                  {stimulus.type === "3d" && (
                    <>
                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <label className="block text-sm mb-1">
                            3D Object
                          </label>

                          <label
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                                flex
                                items-center
                                justify-between
                                cursor-pointer
                              "
                          >
                            <span className="truncate">
                              {stimulus.content || "Choose 3D object"}
                            </span>

                            <Upload size={18} />

                            <input
                              type="file"
                              accept=".glb,.gltf,.obj,.fbx"
                              className="hidden"
                              onChange={(e) => {
                                const file = e.target.files?.[0];

                                if (file) {
                                  updateStimulus(
                                    stimulus.id,
                                    "content",
                                    file.name,
                                  );
                                }
                              }}
                            />
                          </label>
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Duration (seconds)
                          </label>

                          <input
                            type="number"
                            value={stimulus.duration}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "duration",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>
                      </div>

                      <div className="grid grid-cols-2 gap-2 mt-2">
                        <div>
                          <label className="block text-sm mb-1">
                            Position (X)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionX}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionX",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Position (Y)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionY}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionY",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>

                        <div>
                          <label className="block text-sm mb-1">
                            Position (Z)
                          </label>

                          <input
                            type="number"
                            value={stimulus.positionZ}
                            onChange={(e) =>
                              updateStimulus(
                                stimulus.id,
                                "positionZ",
                                e.target.value,
                              )
                            }
                            className="
                                w-full
                                bg-gray-200
                                border
                                border-gray-300
                                rounded-lg
                                px-3
                                py-2
                              "
                          />
                        </div>
                      </div>
                    </>
                  )}
                </div>
              ))}
            </div>
          </section>

          <section
            className="
              border
              border-gray-300
              rounded-lg
              p-4
              mb-5
            "
          >
            <h2 className="text-lg font-medium mb-3">
              Participant Instructions
            </h2>

            <textarea
              value={participantInstructions}
              onChange={(e) => setParticipantInstructions(e.target.value)}
              placeholder="Instructions shown to participants before the experiment begins"
              rows={5}
              className="
                w-full
                bg-gray-200
                border
                border-gray-300
                rounded-lg
                px-3
                py-2
                resize-none
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />
          </section>

          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={saving}
              onClick={saveExperiment}
              className="
                bg-indigo-800
                hover:bg-indigo-700
                disabled:bg-gray-400
                text-white
                rounded-lg
                px-4
                py-2
                flex
                items-center
                gap-2
                transition
              "
            >
              <Plus size={18} />

              {saving
                ? isEditing
                  ? "Updating..."
                  : "Creating..."
                : isEditing
                  ? "Update Experiment"
                  : "Create Experiment"}
            </button>

            <button
              type="button"
              disabled={saving}
              onClick={() => navigate(-1)}
              className="
                border
                border-gray-300
                hover:bg-gray-50
                disabled:bg-gray-100
                rounded-lg
                px-4
                py-2
                transition
              "
            >
              Cancel
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}
