import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import {
  collection,
  getDocs,
  query,
  where,
  addDoc,
  serverTimestamp,
  doc,
  getDoc,
  updateDoc,
} from "firebase/firestore";

import { db } from "../../config/firebase-config";

import { ArrowLeft, Plus, Box, X, Save, Loader2, Trash2 } from "lucide-react";

const CONFIGURATION_ORDER = {
Attentional_Blindness: [
    "ab_AwarenessQuestionText",
    "ab_FadeDelaySeconds",
    "ab_FadeDurationSeconds",
    "ab_InstructionText",
    "ab_NotNoticedText",
    "ab_NoticedText",
    "ab_PostFadePauseSeconds",
    "ab_UseRandomFadeTarget",
    "ab_SpecificFadeTarget",
    "ab_AudioDelaySeconds",
    "ab_UseRandomAudioClip",
    "ab_SpecificAudioClip",
    "ab_AudioLoop",
    "ab_AudioVolume",
    // "ab_SpecificAudioClipIndex",
    "ab_AvailableAudioClips",

    "globalInstructionDelay",
  ],

Depth_Perception: [
    "depth_ActualHeightMeters",
    "depth_MaxHeightMeters",
    "depth_MinHeightMeters",
    "depth_InstructionText",
    "globalInstructionDelay",
  ],
  Distance_Perception: [
    "distance_ActualDistanceMeters",
    "distance_InstructionText",
    "distance_MaxDistanceMeters",
    "distance_MinDistanceMeters",
    "globalInstructionDelay",
  ],

  Memory2: [
    "memory_BriefingText",
    "memory_DistractorInstructionText",
    "memory_DistractorTaskDuration",
    "memory_RandomizeQuestions",
    "memory_RecallInstructionText",
    "memory_RoomInstructionText",
    "memory_TimePerRoomSeconds",
    "memory_TransitionFadeDuration",
    "memory_UseDistractorTask",
    "targets",
  ],

  Memory: [
    "memory_BriefingText",
    "memory_DistractorInstructionText",
    "memory_DistractorTaskDuration",
    "memory_RandomizeQuestions",
    "memory_RecallInstructionText",
    "memory_RoomInstructionText",
    "memory_TimePerRoomSeconds",
    "memory_TransitionFadeDuration",
    "memory_UseDistractorTask",
    "targets",
  ],

  Odd_Item_Detection: [
    "globalInstructionDelay",
    "oddItem_ExcellentThresholdSeconds",
    "oddItem_GoodThresholdSeconds",
    "oddItem_InstructionText",
    "oddItem_RatingExcellent",
    "oddItem_RatingGood",
    "oddItem_RatingKeepPracticing",
    "oddItem_RandomizeTarget",
    "oddItem_RaycastDistance",
    "oddItem_SearchTimeLimitSeconds",
    "oddItem_WrongItemFeedback",
    "oddItem_SelectedTarget",
  ],
};

const ENVIRONMENT_OPTIONS = [
  {
    moduleId: "Depth_Perception",
    name: "Depth Perception (Height)",
    description: "High-altitude rooftop",
  },
  {
    moduleId: "Distance_Perception",
    name: "Distance Perception",
    description: "Roadside setting",
  },
  {
    moduleId: "Attentional_Blindness",
    name: "Inattentional Blindness",
    description: 'Minimalist "minimal" hallway',
  },
  {
    moduleId: "Odd_Item_Detection",
    name: "Odd-item Detection",
    description: "Grocery store setting",
  },
  {
    moduleId: "Memory2",
    name: "Sternberg Memory Scanning",
    description: "House and a diner setting",
  },
  {
    moduleId: "Memory",
    name: "Sternberg Memory Scanning (Diner)",
    description: "A diner setting",
  },
];

const previewSceneMap = {
  Depth_Perception: "DepthPerception_Preview",
  Odd_Item_Detection: "OddItem_Preview",
  Attentional_Blindness: "AttentionalBlindness_Preview",
  Memory2: "MemoryHouse_Preview",
  Distance_Perception: "DistancePerception_Preview",
  Memory: "MemoryDiner_Preview",
};

const cloneConfig = (value) => {
  if (value === undefined || value === null) {
    return {};
  }
  return JSON.parse(JSON.stringify(value));
};

const formatConfigLabel = (key) => {
  let label = String(key)
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ")
    .replace(/-/g, " ");

  label = label
    .replace(/^ab /i, "")
    .replace(/^depth /i, "")
    .replace(/^memory /i, "")
    .replace(/^oddItem /i, "")
    .replace(/^odd item /i, "");

  // nicer names for the new audio fields
  if (key === "ab_AudioDelaySeconds") return "Audio Delay (seconds)";
  if (key === "ab_UseRandomAudioClip") return "Use Random Audio Clip";
  if (key === "ab_SpecificAudioClipName") return "Selected Audio Clip";
  if (key === "ab_AudioLoop") return "Loop Audio";
  if (key === "ab_AudioVolume") return "Audio Volume";

  return label.replace(/\b\w/g, (character) => character.toUpperCase()).trim();
};
const isLongTextKey = (key) => {
  const lower = String(key).toLowerCase();
  return (
    lower.includes("instruction") ||
    lower.includes("question") ||
    lower.includes("briefing") ||
    lower.includes("description") ||
    lower.includes("feedback") ||
    lower.includes("rating") ||
    lower.includes("text")
  );
};

export default function InstructorExperimentBuilder() {
  const navigate = useNavigate();
  const { blockId, experimentId } = useParams();

  const isEditing = Boolean(experimentId);

  const [experimentName, setExperimentName] = useState("");
  const [instructions, setInstructions] = useState("");

  const [environment, setEnvironment] = useState("");
  const [moduleId, setModuleId] = useState("");
  const [moduleName, setModuleName] = useState("");
  const [sceneId, setSceneId] = useState("");
  const [moduleDescription, setModuleDescription] = useState("");
  const [configuration, setConfiguration] = useState({});
  const [moduleLoading, setModuleLoading] = useState(true);

  const [groups, setGroups] = useState([]);
  const [selectedGroups, setSelectedGroups] = useState([]);
  const [vrDevices, setVrDevices] = useState([]);
  const [selectedVrId, setSelectedVrId] = useState("");
  const [allowStudentExperiments, setAllowStudentExperiments] = useState(false);

  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(isEditing);

  const [showPreview, setShowPreview] = useState(false);

  const targetRefs = useRef({});
  const [newTargetIndex, setNewTargetIndex] = useState(null);
  const [activeTargetIndex, setActiveTargetIndex] = useState(null);

  const getPreviewSceneName = () => {
    if (!moduleId && !sceneId) return null;
    const key = moduleId || sceneId;
    return previewSceneMap[key] || null;
  };

  const loadExperimentModule = async (selectedModuleId) => {
    if (!selectedModuleId) return null;

    try {
      const moduleSnap = await getDoc(doc(db, "experimentModule", selectedModuleId));

      if (!moduleSnap || !moduleSnap.exists()) {
        console.error(`Experiment module "${selectedModuleId}" was not found.`);
        return null;
      }

      const data = moduleSnap.data();

      return {
        id: selectedModuleId,
        moduleName: data.moduleName || "",
        description: data.description || "",
        sceneId: data.sceneId || "",
        defaultConfig: cloneConfig(data.defaultConfig || {}),
      };
    } catch (error) {
      console.error("Error loading experiment module:", error);
      return null;
    }
  };

  const selectEnvironment = async (option) => {
    setModuleLoading(true);
    setShowPreview(false);

    try {
      const module = await loadExperimentModule(option.moduleId);

      if (!module) {
        alert(`Unable to load default configuration for ${option.name}.`);
        return;
      }

      setEnvironment(option.name);
      setModuleId(module.id);
      setModuleName(module.moduleName || option.name);
      setExperimentName(module.moduleName || option.name);
      setSceneId(module.sceneId || "");
      setModuleDescription(module.description || option.description);
      setInstructions(module.description || option.description);

      setConfiguration(cloneConfig(module.defaultConfig || {}));
    } finally {
      setModuleLoading(false);
    }
  };
  useEffect(() => {
  const getVrDevices = async () => {
    try {
      const snapshot = await getDocs(collection(db, "vrDevices"));
      const list = snapshot.docs.map((d) => ({
        id: d.id,
        ...d.data(),
      }));
      list.sort((a, b) => (a.vrId || a.id).localeCompare(b.vrId || b.id));
      setVrDevices(list);
    } catch (error) {
      console.error("Error loading VR devices:", error);
    }
  };
  getVrDevices();
}, []);


  useEffect(() => {
    const getGroups = async () => {
      if (!blockId) return;

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

    getGroups();
  }, [blockId]);

  useEffect(() => {
    if (newTargetIndex === null) return;
    const target = targetRefs.current[newTargetIndex];
    if (!target) return;

    target.scrollIntoView({ behavior: "smooth", block: "center" });
    setActiveTargetIndex(newTargetIndex);

    const timeout = setTimeout(() => {
      setNewTargetIndex(null);
    }, 1200);

    return () => clearTimeout(timeout);
  }, [newTargetIndex, configuration.targets]);
const toggleVrDevice = (deviceId) => {
  setSelectedVrDevices((prev) => {
    if (prev.includes(deviceId)) {
      return prev.filter((id) => id !== deviceId);
    }
    return [...prev, deviceId];
  });
};
  useEffect(() => {
    const getExperiment = async () => {
      if (!experimentId) {
        setLoading(false);
        setModuleLoading(false);
        return;
      }
      setSelectedVrId(data.vrId || "");
      try {
        setLoading(true);

        const experimentRef = doc(db, "experiment", experimentId);
        const experimentSnap = await getDoc(experimentRef);

        if (!experimentSnap.exists()) {
          alert("Experiment not found.");
          navigate(-1);
          return;
        }

        const data = experimentSnap.data();

        setExperimentName(data.experimentName || "");
        setInstructions(data.instructions || "");
        setEnvironment(data.environment || "");
        setSelectedGroups(data.groupIds || []);
        setAllowStudentExperiments(data.allowStudentExperiments === true);

        const savedModuleId = data.moduleId || "";
        const savedEnvironment = data.environment || "";

        let option = ENVIRONMENT_OPTIONS.find(
          (item) =>
            item.moduleId === savedModuleId || item.name === savedEnvironment,
        );

        if (option) {
          const module = await loadExperimentModule(
            savedModuleId || option.moduleId,
          );

          if (module) {
            setModuleId(module.id);
            setModuleName(module.moduleName || option.name);
            setExperimentName(data.experimentName || module.moduleName || option.name);
            setSceneId(module.sceneId || "");
            setModuleDescription(module.description || option.description);
            setInstructions(data.instructions || module.description || option.description);

            if (data.configuration && typeof data.configuration === "object") {
              setConfiguration(cloneConfig(data.configuration));
            } else {
              setConfiguration(cloneConfig(module.defaultConfig || {}));
            }
          }
        } else if (data.configuration && typeof data.configuration === "object") {
          setConfiguration(cloneConfig(data.configuration));
        }
      } catch (error) {
        console.error("Error getting experiment:", error);
        alert("Unable to load the experiment.");
        navigate(-1);
      } finally {
        setLoading(false);
        setModuleLoading(false);
      }
    };

    getExperiment();
  }, [experimentId, navigate]);

  const updateConfigValue = (path, value) => {
    setConfiguration((previous) => {
      const next = cloneConfig(previous);
      let current = next;

      for (let index = 0; index < path.length - 1; index += 1) {
        current = current[path[index]];
      }

      current[path[path.length - 1]] = value;
      return next;
    });
  };

  const toggleGroup = (groupId) => {
    setSelectedGroups((previous) => {
      if (previous.includes(groupId)) {
        return previous.filter((id) => id !== groupId);
      }
      return [...previous, groupId];
    });
  };

  const getOrderedConfigurationKeys = () => {
    const configuredOrder = CONFIGURATION_ORDER[moduleId];
    if (!configuredOrder) {
      return Object.keys(configuration);
    }

    const orderedKeys = configuredOrder.filter((key) =>
      Object.prototype.hasOwnProperty.call(configuration, key),
    );

    const remainingKeys = Object.keys(configuration).filter(
      (key) => !configuredOrder.includes(key),
    );

    return [...orderedKeys, ...remainingKeys];
  };

  const getOrderedTargetKeys = (target) => {
    const hiddenKeys = ["room", "gameId", "objectName"];
    const preferredOrder = [
      "questions",
      "name",
      "targetName",
      "question",
      "answer",
      "correctAnswer",
      "description",
      "text",
      "value",
      "type",
    ];

    const existingKeys = Object.keys(target).filter(
      (key) => !hiddenKeys.includes(key),
    );

    const orderedKeys = preferredOrder.filter((key) =>
      existingKeys.includes(key),
    );

    const remainingKeys = existingKeys.filter(
      (key) => !preferredOrder.includes(key),
    );

    return [...orderedKeys, ...remainingKeys];
  };

  const addMemoryQuestion = (questionsPath) => {
    setConfiguration((previous) => {
      const next = cloneConfig(previous);
      let questions = next;

      for (let index = 0; index < questionsPath.length; index += 1) {
        const pathKey = questionsPath[index];
        if (questions[pathKey] === undefined) {
          questions[pathKey] = [];
        }
        questions = questions[pathKey];
      }

      if (!Array.isArray(questions)) return previous;

      questions.push({
        questionText: "",
        questionType: "MultipleChoice",
        multipleChoiceOptions: ["", ""],
        correctAnswer: "",
        enabled: true,
      });

      return next;
    });
  };

  const removeMemoryQuestion = (questionsPath, questionIndex) => {
    setConfiguration((previous) => {
      const next = cloneConfig(previous);
      let questions = next;

      for (let index = 0; index < questionsPath.length; index += 1) {
        questions = questions[questionsPath[index]];
      }

      if (!Array.isArray(questions)) return previous;

      questions.splice(questionIndex, 1);
      return next;
    });
  };

  const validateMemoryQuestions = () => {
    if (moduleId !== "Memory2" && moduleId !== "Memory") return true;

    const targets = Array.isArray(configuration.targets) ? configuration.targets : [];

    for (let targetIndex = 0; targetIndex < targets.length; targetIndex += 1) {
      const target = targets[targetIndex];
      const questions = target && Array.isArray(target.questions) ? target.questions : [];

      for (let questionIndex = 0; questionIndex < questions.length; questionIndex += 1) {
        const question = questions[questionIndex];

        if (!question || typeof question !== "object") {
          alert(`Target ${targetIndex + 1}, Question ${questionIndex + 1} is invalid.`);
          return false;
        }

        if (typeof question.questionText !== "string" || !question.questionText.trim()) {
          alert(`Please enter question text for Target ${targetIndex + 1}, Question ${questionIndex + 1}.`);
          return false;
        }

        if (typeof question.correctAnswer !== "string" || !question.correctAnswer.trim()) {
          alert(`Please enter correct answer for Target ${targetIndex + 1}, Question ${questionIndex + 1}.`);
          return false;
        }

        if (question.questionType === "MultipleChoice") {
          const options = Array.isArray(question.multipleChoiceOptions) ? question.multipleChoiceOptions : [];
          if (options.length === 0) {
            alert(`Please add options for Target ${targetIndex + 1}, Question ${questionIndex + 1}.`);
            return false;
          }
          if (options.some((opt) => typeof opt !== "string" || !opt.trim())) {
            alert(`Please fill all choice options for Target ${targetIndex + 1}, Question ${questionIndex + 1}.`);
            return false;
          }
        }
      }
    }

    return true;
  };

  const addMemoryTarget = () => {
    setConfiguration((previous) => {
      const next = cloneConfig(previous);
      const targets = Array.isArray(next.targets) ? next.targets : [];

      let newTarget = {
        objectName: "",
        questions: [
          {
            questionText: "",
            questionType: "MultipleChoice",
            multipleChoiceOptions: ["", ""],
            correctAnswer: "",
            enabled: true,
          },
        ],
      };

      targets.push(newTarget);
      next.targets = targets;
      return next;
    });

    setNewTargetIndex(
      Array.isArray(configuration.targets) ? configuration.targets.length : 0,
    );
  };

  const removeMemoryTarget = (targetIndex) => {
    setConfiguration((previous) => {
      const next = cloneConfig(previous);
      if (Array.isArray(next.targets)) {
        next.targets.splice(targetIndex, 1);
      }
      return next;
    });
  };

const handleSaveExperiment = async (event) => {
  event.preventDefault();

  if (!experimentName.trim()) {
    alert("Please enter an experiment name.");
    return;
  }

  if (!moduleId) {
    alert("Please select an environment / module.");
    return;
  }
if (!selectedVrId) {
      alert("Please select a VR device assignment.");
      return;
    }
  if (moduleId === "Attentional_Blindness") {
    const isRandom = configuration.ab_UseRandomFadeTarget === true;
    if (!isRandom && !configuration.ab_SpecificFadeTarget) {
      alert("Please select a Fade Target (or enable Use Random Fade Target).");
      return;
    }
  }

 if (moduleId === "Odd_Item_Detection") {
  const isRandom = configuration.oddItem_RandomizeTarget === true;
  if (!isRandom && !configuration.oddItem_SelectedTarget) {
    alert("Please select a Target (or enable Randomize Target).");
    return;
  }
}

  if (!validateMemoryQuestions()) {
    return;
  }

    setSaving(true);

    try {
      const payload = {
        blockId: blockId || null,
        experimentName: experimentName.trim(),
        instructions: instructions.trim(),
        environment: environment || "",
        moduleId: moduleId || "",
        moduleName: moduleName || "",
        vrId: selectedVrId || "",
        sceneId: sceneId || "",
        moduleDescription: moduleDescription || "",
        configuration: configuration || {},
        groupIds: selectedGroups || [],
        allowStudentExperiments: allowStudentExperiments === true,
        updatedAt: serverTimestamp(),
      };

      if (isEditing) {
        await updateDoc(doc(db, "experiment", experimentId), payload);
        alert("Experiment updated successfully.");
      } else {
        payload.createdAt = serverTimestamp();
        await addDoc(collection(db, "experiment"), payload);
        alert("Experiment created successfully.");
      }

      navigate(-1);
    } catch (error) {
      console.error("Error saving experiment:", error);
      alert("Failed to save experiment. Please try again.");
    } finally {
      setSaving(false);
    }
  };

  const renderMemoryQuestions = (questions, path) => {
    return (
      <>
        <div className="md:col-span-2 flex justify-end">
          <button
            type="button"
            disabled={saving}
            onClick={() => addMemoryQuestion(path)}
            className="flex items-center gap-1.5 rounded-lg border border-gray-300 px-3 py-1.5 text-xs font-medium text-gray-700 transition hover:bg-gray-50 disabled:opacity-50"
          >
            <Plus size={14} /> Add Question
          </button>
        </div>

        {questions.length === 0 ? (
          <div className="md:col-span-2 rounded-lg border border-dashed border-gray-300 bg-white p-6 text-center text-sm text-gray-500">
            No questions configured.
          </div>
        ) : (
          <div className="md:col-span-2 space-y-4">
            {questions.map((question, index) => (
              <div key={`${path.join(".")}-${index}`} className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
                <div className="flex items-center justify-between border-b border-gray-200 bg-gray-50 px-4 py-3">
                  <div className="flex items-center gap-3">
                    <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-100 text-sm font-semibold text-indigo-700">
                      {index + 1}
                    </div>
                    <p className="text-sm font-semibold text-gray-900">Question {index + 1}</p>
                  </div>
                  <button
                    type="button"
                    disabled={saving}
                    onClick={() => removeMemoryQuestion(path, index)}
                    className="rounded-lg px-2.5 py-1.5 text-xs font-medium text-red-600 transition hover:bg-red-50 disabled:opacity-50"
                  >
                    Remove
                  </button>
                </div>

                <div className="grid grid-cols-1 gap-4 p-4 md:grid-cols-2">
                  <div className="md:col-span-2">
                    <label className="mb-1.5 block text-sm font-medium text-gray-700">Question Text</label>
                    <textarea
                      value={question.questionText ?? ""}
                      onChange={(e) => updateConfigValue([...path, index, "questionText"], e.target.value)}
                      rows={2}
                      placeholder="Enter the question..."
                      className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
                    />
                  </div>

                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-gray-700">Question Type</label>
                    <select
                      value={question.questionType === "YesNo" ? "YesNo" : "MultipleChoice"}
                      onChange={(e) => {
                        const newType = e.target.value;
                        updateConfigValue([...path, index, "questionType"], newType);
                        const optionsPath = [...path, index, "multipleChoiceOptions"];
                        if (newType === "MultipleChoice") {
                          if (!Array.isArray(question.multipleChoiceOptions)) {
                            updateConfigValue(optionsPath, ["", ""]);
                          }
                        } else {
                          updateConfigValue(optionsPath, []);
                        }
                      }}
                      className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
                    >
                      <option value="MultipleChoice">Multiple Choice</option>
                      <option value="YesNo">Yes / No</option>
                    </select>
                  </div>

                  <div>
                    <label className="mb-1.5 block text-sm font-medium text-gray-700">Correct Answer</label>
                    <input
                      type="text"
                      value={question.correctAnswer ?? ""}
                      onChange={(e) => updateConfigValue([...path, index, "correctAnswer"], e.target.value)}
                      placeholder="Correct answer..."
                      className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
                    />
                  </div>

                  {question.questionType === "MultipleChoice" && (
                    <div className="md:col-span-2">
                      <div className="mb-2 flex items-center justify-between">
                        <label className="block text-sm font-medium text-gray-700">Options</label>
                        <button
                          type="button"
                          onClick={() => {
                            const options = Array.isArray(question.multipleChoiceOptions) ? question.multipleChoiceOptions : [];
                            updateConfigValue([...path, index, "multipleChoiceOptions"], [...options, ""]);
                          }}
                          className="flex items-center gap-1 rounded-lg border border-gray-300 px-2.5 py-1 text-xs font-medium text-gray-700 hover:bg-gray-50"
                        >
                          <Plus size={12} /> Add Option
                        </button>
                      </div>

                      <div className="space-y-2">
                        {(question.multipleChoiceOptions || []).map((opt, optIndex) => (
                          <div key={optIndex} className="flex items-center gap-2">
                            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-50 text-xs font-medium text-indigo-700">
                              {optIndex + 1}
                            </span>
                            <input
                              type="text"
                              value={opt ?? ""}
                              onChange={(e) => {
                                const nextOpts = [...question.multipleChoiceOptions];
                                nextOpts[optIndex] = e.target.value;
                                updateConfigValue([...path, index, "multipleChoiceOptions"], nextOpts);
                              }}
                              placeholder={`Option ${optIndex + 1}`}
                              className="w-full rounded-lg border border-gray-300 p-2 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
                            />
                            <button
                              type="button"
                              onClick={() => {
                                const nextOpts = question.multipleChoiceOptions.filter((_, i) => i !== optIndex);
                                updateConfigValue([...path, index, "multipleChoiceOptions"], nextOpts);
                              }}
                              className="p-2 text-gray-400 hover:text-red-600"
                            >
                              <X size={16} />
                            </button>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </>
    );
  };

  const previewSceneName = getPreviewSceneName();

  if (loading) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center bg-gray-50">
        <Loader2 className="animate-spin text-indigo-600" size={32} />
      </div>
    );
  }

  return (
    <div className="font-google min-h-screen bg-gray-50 text-black pb-24">
      <main className="pt-20 px-5 max-w-5xl mx-auto">
        <button
          onClick={() => navigate(-1)}
          className="flex items-center gap-2 text-gray-600 hover:text-black transition mb-6"
        >
          <ArrowLeft size={20} />
          <span>Back</span>
        </button>

        <h1 className="text-2xl font-medium mb-6">
          {isEditing ? "Edit Experiment" : "Create New Experiment"}
        </h1>

        {/* Environment Selection */}
        <section className="bg-white border border-gray-200 rounded-xl p-6 mb-6 shadow-sm">
          <h2 className="text-lg font-medium mb-3">Select Environment / Module</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {ENVIRONMENT_OPTIONS.map((opt) => {
              const isSelected = moduleId === opt.moduleId || environment === opt.name;
              return (
                <button
                  key={opt.moduleId}
                  type="button"
                  onClick={() => selectEnvironment(opt)}
                  className={`text-left border rounded-lg p-4 transition ${
                    isSelected
                      ? "border-indigo-600 bg-indigo-50/50 ring-2 ring-indigo-500/20"
                      : "border-gray-200 hover:border-gray-300 hover:bg-gray-50"
                  }`}
                >
                  <p className="font-medium text-sm text-gray-900">{opt.name}</p>
                  <p className="text-xs text-gray-500 mt-1">{opt.description}</p>
                </button>
              );
            })}
          </div>
        </section>

        {/* Experiment Details */}
        <section className="bg-white border border-gray-200 rounded-xl p-6 mb-6 shadow-sm">
          <h2 className="text-lg font-medium mb-4">Experiment Information</h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Experiment Title
              </label>
              <input
                type="text"
                value={experimentName}
                onChange={(e) => setExperimentName(e.target.value)}
                placeholder="e.g. Depth Perception - Trial A"
                className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Instructions / Participant Guidance
              </label>
              <textarea
                value={instructions}
                onChange={(e) => setInstructions(e.target.value)}
                rows={3}
                placeholder="Instructions shown to students before taking the experiment..."
                className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
              />
            </div>
          </div>
        </section>

{/* VR Environment Preview */}
        {moduleId && (
          <section className="bg-white border border-gray-200 rounded-xl p-6 mb-6 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="text-lg font-medium">VR Environment Preview</h2>
                <p className="text-sm text-gray-500">
                  Click inside the preview window, To exit, press Esc key twice
                </p>
              </div>
              {showPreview && (
                <button
                  onClick={() => setShowPreview(false)}
                  className="text-gray-500 hover:text-black transition"
                >
                  <X size={20} />
                </button>
              )}
            </div>

            <div className="h-[600px] bg-gray-100 rounded-lg overflow-hidden relative border border-gray-200">
              {!showPreview ? (
                <button
                  onClick={() => setShowPreview(true)}
                  disabled={!previewSceneName}
                  className="w-full h-full flex flex-col items-center justify-center text-gray-500 hover:bg-gray-200 transition disabled:opacity-50"
                >
                  <Box size={48} className="mb-3 text-indigo-600" />
                  <p className="font-medium text-lg text-gray-900">
                    {previewSceneName ? "Click to Load 3D Preview" : "Preview not available"}
                  </p>
                  <p className="text-sm text-gray-500 mt-1">
                    Loads real-time WebGL view of {environment || "selected environment"}
                  </p>
                </button>
              ) : (
                <iframe
                  src={`/previews/all-experiments/index.html?scene=${previewSceneName}`}
                  className="w-full h-full border-0"
                  allow="fullscreen; pointer-lock"
                  title="Environment Preview"
                />
              )}
            </div>

            <p className="text-sm text-gray-500 mt-3">
              (Note: Preview shows only the base environment and not configured variables)
            </p>
          </section>
        )}
        {/* Configuration Parameters */}
        {moduleId && Object.keys(configuration).length > 0 && (
          <section className="bg-white border border-gray-200 rounded-xl p-6 mb-6 shadow-sm">
            <h2 className="text-lg font-medium mb-4">Module Configuration Parameters</h2>
            <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
              {getOrderedConfigurationKeys().map((key) => {
                const value = configuration[key];

                // Memory targets
                if (key === "targets" && Array.isArray(value)) {
                  return (
                    <div key={key} className="md:col-span-2 space-y-4 pt-4 border-t border-gray-200">
                      <div className="flex items-center justify-between">
                        <h3 className="text-md font-semibold text-gray-800">Memory Scan Targets</h3>
                        <button
                          type="button"
                          onClick={addMemoryTarget}
                          className="flex items-center gap-1.5 rounded-lg bg-indigo-50 border border-indigo-200 px-3 py-1.5 text-xs font-semibold text-indigo-700 transition hover:bg-indigo-100"
                        >
                          <Plus size={14} /> Add Target Object
                        </button>
                      </div>

                      {value.map((target, targetIdx) => (
                        <div
                          key={targetIdx}
                          ref={(el) => (targetRefs.current[targetIdx] = el)}
                          className={`rounded-xl border p-5 transition ${
                            activeTargetIndex === targetIdx
                              ? "border-indigo-500 bg-indigo-50/30 ring-2 ring-indigo-500/20"
                              : "border-gray-200 bg-gray-50/50"
                          }`}
                        >
                          <div className="flex items-center justify-between mb-4">
                            <span className="font-semibold text-gray-700 text-sm">
                              Target #{targetIdx + 1}
                            </span>
                            <button
                              type="button"
                              onClick={() => removeMemoryTarget(targetIdx)}
                              className="text-red-600 hover:text-red-700 p-1 rounded-lg hover:bg-red-50"
                            >
                              <Trash2 size={16} />
                            </button>
                          </div>

                          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 mb-4">
                            {getOrderedTargetKeys(target).map((tKey) => {
                              if (tKey === "questions") return null;
                              return (
                                <div key={tKey}>
                                  <label className="block text-xs font-medium text-gray-600 mb-1 capitalize">
                                    {formatConfigLabel(tKey)}
                                  </label>
                                  <input
                                    type="text"
                                    value={target[tKey] ?? ""}
                                    onChange={(e) =>
                                      updateConfigValue(["targets", targetIdx, tKey], e.target.value)
                                    }
                                    className="w-full rounded-lg border border-gray-300 p-2 text-sm bg-white"
                                  />
                                </div>
                              );
                            })}
                          </div>

                          {Array.isArray(target.questions) && (
                            <div className="space-y-3 pt-3 border-t border-gray-200">
                              <h4 className="text-xs font-bold uppercase tracking-wider text-gray-500">
                                Target Questions
                              </h4>
                              {renderMemoryQuestions(target.questions, ["targets", targetIdx, "questions"])}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  );
                }

               if (
  key === "ab_FurnitureList" ||
  key === "oddItem_AvailableTargets" ||
  key === "oddItem_TargetDisplayName" ||
  key === "depth_StepAmount" ||
  key === "depth_MaxHeightMeters" ||
  key === "depth_MinHeightMeters" ||
  key === "distance_MinDistanceMeters" ||
  key === "distance_MaxDistanceMeters" ||
  key === "globalInstructionDelay" ||
  key === "ab_AvailableAudioClips" ||          
  key === "ab_SpecificAudioClipIndex"
) {
  return null;
}

// Attentional Blindness – Selected Fade Target
if (key === "ab_SpecificFadeTarget") {
  const isRandom = configuration.ab_UseRandomFadeTarget === true;
  const furniture = Array.isArray(configuration.ab_FurnitureList)
    ? configuration.ab_FurnitureList
    : [];

  return (
    <div key={key}>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        Selected Fade Target
      </label>
      <select
        value={configuration[key] ?? ""}
        disabled={isRandom}
        onChange={(e) => updateConfigValue([key], e.target.value)}
        className={`w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 ${
          isRandom
            ? "bg-gray-100 text-gray-400 cursor-not-allowed"
            : "bg-white"
        }`}
      >
        <option value="">— Select a target —</option>
        {furniture.map((item) => (
          <option key={item} value={item}>
            {item}
          </option>
        ))}
      </select>
      {isRandom && (
        <p className="mt-1 text-xs text-gray-500">
          Disabled while “Use Random Fade Target” is enabled.
        </p>
      )}
    </div>
  );
}

// Attentional Blindness – Selected Audio Clip (Dropdown)
// Attentional Blindness – Selected Audio Clip (Dropdown)
if (key === "ab_SpecificAudioClip") {
  const isRandom = configuration.ab_UseRandomAudioClip === true;
  const availableClips = Array.isArray(configuration.ab_AvailableAudioClips)
    ? configuration.ab_AvailableAudioClips
    : [];

  return (
    <div key={key}>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        Selected Audio Clip
      </label>
      <select
        value={configuration[key] ?? ""}
        disabled={isRandom}
        onChange={(e) => updateConfigValue([key], e.target.value)}
        className={`w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 ${
          isRandom
            ? "bg-gray-100 text-gray-400 cursor-not-allowed"
            : "bg-white"
        }`}
      >
        <option value="">— Select a sound —</option>
        {availableClips.map((clip) => (
          <option key={clip} value={clip}>
            {clip}
          </option>
        ))}
      </select>
      {isRandom && (
        <p className="mt-1 text-xs text-gray-500">
          Disabled while “Use Random Audio Clip” is enabled.
        </p>
      )}
    </div>
  );
}
                // Odd Item Detection - Selected Target (Dropdown)
// Odd Item Detection - Selected Target (Dropdown)
if (key === "oddItem_SelectedTarget") {
  const isRandom = configuration.oddItem_RandomizeTarget === true;
  const available = Array.isArray(configuration.oddItem_AvailableTargets)
    ? configuration.oddItem_AvailableTargets
    : [];

  return (
    <div key={key}>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        Selected Target
      </label>
      <select
        value={value ?? ""}
        disabled={isRandom}
        onChange={(e) => updateConfigValue([key], e.target.value)}
        className={`w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 ${
          isRandom
            ? "bg-gray-100 text-gray-400 cursor-not-allowed"
            : "bg-white"
        }`}
      >
        <option value="">— Select a target —</option>
        {available.map((item) => (
          <option key={item} value={item}>
            {item}  
          </option>
        ))}
      </select>
      {isRandom && (
        <p className="mt-1 text-xs text-gray-500">
          Disabled while “Randomize Target” is enabled.
        </p>
      )}
    </div>
  );
}

                // Boolean
                if (typeof value === "boolean") {
                  return (
                    <div key={key} className="flex items-center gap-3">
                      <input
                        type="checkbox"
                        id={key}
                        checked={value}
                        onChange={(e) => updateConfigValue([key], e.target.checked)}
                        className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                      />
                      <label htmlFor={key} className="text-sm font-medium text-gray-700 cursor-pointer">
                        {formatConfigLabel(key)}
                      </label>
                    </div>
                  );
                }

                // Long text
                if (isLongTextKey(key)) {
                  return (
                    <div key={key} className="md:col-span-2">
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        {formatConfigLabel(key)}
                      </label>
                      <textarea
                        value={value ?? ""}
                        onChange={(e) => updateConfigValue([key], e.target.value)}
                        rows={2}
                        className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
                      />
                    </div>
                  );
                }
if (
  key === "distance_MinDistanceMeters" ||
  key === "distance_MaxDistanceMeters"
) {
  return null;
}

if (key === "distance_ActualDistanceMeters") {
  const min = Number(configuration.distance_MinDistanceMeters) || 0;
  const max = Number(configuration.distance_MaxDistanceMeters) || 999;

  return (
    <div key={key}>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {formatConfigLabel(key)}
      </label>
      <input
        type="number"
        value={value ?? ""}
        min={min}
        max={max}
        onChange={(e) => {
          let num = Number(e.target.value);
          if (isNaN(num)) num = min;
          num = Math.min(Math.max(num, min), max); // clamp
          updateConfigValue([key], num);
        }}
        className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
      />
      <p className="mt-1 text-xs text-gray-500">
        Must be between {min} m and {max} m
      </p>
    </div>
  );
}

if (key === "depth_ActualHeightMeters" || key === "depth_MinHeightMeters") {
  const min = key === "depth_ActualHeightMeters" ? Number(configuration.depth_MinHeightMeters) || 10 : 0;
  const max = Number(configuration.depth_MaxHeightMeters) || 28;

  return (
    <div key={key}>
      <label className="block text-sm font-medium text-gray-700 mb-1">
        {formatConfigLabel(key)}
      </label>
      <input
        type="number"
        value={value ?? ""}
        min={min}
        max={max}
        onChange={(e) => {
          let num = Number(e.target.value);
          if (isNaN(num)) num = min;
          num = Math.min(Math.max(num, min), max);
          updateConfigValue([key], num);
        }}
        className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
      />
      <p className="mt-1 text-xs text-gray-500">
        Must be between {min} m and {max} m
      </p>
    </div>
  );
}
                // Default
                return (
                  <div key={key}>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {formatConfigLabel(key)}
                    </label>
                    <input
                      type={typeof value === "number" ? "number" : "text"}
                      value={value ?? ""}
                      onChange={(e) =>
                        updateConfigValue(
                          [key],
                          typeof value === "number" ? Number(e.target.value) : e.target.value,
                        )
                      }
                      className="w-full rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20"
                    />
                  </div>
                );
              })}
            </div>
          </section>
        )}

        {/* Group Assignment */}
        {groups.length > 0 && (
          <section className="bg-white border border-gray-200 rounded-xl p-6 mb-6 shadow-sm">
            <h2 className="text-lg font-medium mb-3">Assign Student Groups</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
              {groups.map((group) => {
                const isChecked = selectedGroups.includes(group.id);
                return (
                  <label
                    key={group.id}
                    onClick={() => toggleGroup(group.id)}
                    className={`flex items-center gap-3 border rounded-lg p-3 cursor-pointer transition ${
                      isChecked ? "border-indigo-600 bg-indigo-50/50" : "border-gray-200 hover:bg-gray-50"
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={isChecked}
                      onChange={() => {}}
                      className="h-4 w-4 rounded text-indigo-600 focus:ring-indigo-500"
                    />
                    <span className="text-sm font-medium text-gray-800">
                      {group.groupName || group.name || `Group ${group.id}`}
                    </span>
                  </label>
                );
              })}
            </div>
          </section>
        )}
{/* VR Device Assignment */}
<section className="bg-white border border-gray-200 rounded-xl p-6 mb-6 shadow-sm">
  <h2 className="text-lg font-medium mb-3">Assign VR Device</h2>
  <p className="text-sm text-gray-500 mb-3">
    Select the physical headset this experiment will run on.
  </p>

<select
    value={selectedVrId}
    onChange={(e) => setSelectedVrId(e.target.value)}
    required
    className="w-full max-w-xs rounded-lg border border-gray-300 p-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 bg-white"
  >
    <option value="" disabled>— Select a VR device —</option>
    {vrDevices.map((device) => {
      const label = device.vrId || device.id;
      const status = device.Status || device.status || "";
      return (
        <option key={device.id} value={device.vrId || device.id}>
          {label}{status ? ` (${status})` : ""}
        </option>
      );
    })}
  </select>
  </section>
        {/* Permissions */}
        <section className="bg-white border border-gray-200 rounded-xl p-6 mb-8 shadow-sm">
          <h2 className="text-lg font-medium mb-3">Permissions</h2>
          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={allowStudentExperiments}
              onChange={(e) => setAllowStudentExperiments(e.target.checked)}
              className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
            />
            <span className="text-sm font-medium text-gray-700">
              Allow students to create and run their own experiments
            </span>
          </label>
        </section>

        {/* Bottom Save Bar */}
        <div className="sticky bottom-5 z-10 flex items-center justify-end gap-3 bg-white/95 backdrop-blur border border-gray-200 p-4 rounded-xl shadow-lg">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="rounded-xl border border-gray-300 px-5 py-2.5 font-medium text-gray-700 transition hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            onClick={handleSaveExperiment}
            disabled={saving}
            className="flex items-center gap-2 rounded-xl bg-indigo-600 px-6 py-2.5 font-medium text-white shadow-sm transition hover:bg-indigo-700 disabled:opacity-60"
          >
            {saving ? <Loader2 size={18} className="animate-spin" /> : <Save size={18} />}
            <span>{isEditing ? "Update Experiment" : "Publish Experiment"}</span>
          </button>
        </div>
      </main>
    </div>
  );
}