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

import { ArrowLeft, Plus } from "lucide-react";

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
    "globalInstructionDelay",
  ],

  Depth_Perception: [
    "depth_ActualHeightMeters",
    "depth_InstructionText",
    "depth_MaxHeightMeters",
    "depth_StepAmount",
    "globalInstructionDelay",
  ],

  Depth_Perception2: [
    "depth_ActualDistanceMeters",
    "depth_InstructionText",
    "depth_MaxDistanceMeters",
    "depth_MinDistanceMeters",
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

  Odd_Item_Detection: [
    "globalInstructionDelay",
    "oddItem_ExcellentThresholdSeconds",
    "oddItem_GoodThresholdSeconds",
    "oddItem_InstructionText",
    "oddItem_RatingExcellent",
    "oddItem_RatingGood",
    "oddItem_RatingKeepPracticing",
    "oddItem_RaycastDistance",
    "oddItem_SearchTimeLimitSeconds",
    "oddItem_WrongItemFeedback",
  ],
};

const ENVIRONMENT_OPTIONS = [
  {
    moduleId: "Depth_Perception",
    name: "Depth Perception (Height)",
    description: "High-altitude rooftop",
  },
  {
    moduleId: "Depth_Perception2",
    name: "Depth Perception (Distance)",
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
];

const QUESTION_FIELD_ORDER = [
  "questionText",
  "questionType",
  "multipleChoiceOptions",
  "correctAnswer",
  "enabled",
];

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

  const [allowStudentExperiments, setAllowStudentExperiments] = useState(false);

  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(isEditing);

  const targetRefs = useRef({});
  const [newTargetIndex, setNewTargetIndex] = useState(null);
  const [activeTargetIndex, setActiveTargetIndex] = useState(null);

  const loadExperimentModule = async (selectedModuleId) => {
    if (!selectedModuleId) {
      return null;
    }

    try {
      const moduleSnap = await getDoc(
        doc(db, "experimentModule", selectedModuleId),
      );

      if (!moduleSnap.exists()) {
        console.error(`Experiment module "${selectedModuleId}" was not found.`);

        return null;
      }

      const data = moduleSnap.data();

      return {
        id: moduleSnap.id,
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

    try {
      const module = await loadExperimentModule(option.moduleId);

      if (!module) {
        alert(`Unable to load the default configuration for ${option.name}.`);

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
    const getGroups = async () => {
      if (!blockId) {
        return;
      }

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

    target.scrollIntoView({
      behavior: "smooth",
      block: "center",
    });

    setActiveTargetIndex(newTargetIndex);

    const timeout = setTimeout(() => {
      setNewTargetIndex(null);
    }, 1200);

    return () => clearTimeout(timeout);
  }, [newTargetIndex, configuration.targets]);

  useEffect(() => {
    const getExperiment = async () => {
      if (!experimentId) {
        setLoading(false);
        setModuleLoading(false);
        return;
      }

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

        if (!option && savedModuleId === "Memory2") {
          option = ENVIRONMENT_OPTIONS.find(
            (item) => item.moduleId === "Memory2",
          );
        }

        if (option) {
          const module = await loadExperimentModule(
            savedModuleId || option.moduleId,
          );

          if (module) {
            if (module) {
              setModuleId(module.id);

              setModuleName(module.moduleName || option.name);
              setExperimentName(module.moduleName || option.name);

              setSceneId(module.sceneId || "");

              setModuleDescription(module.description || option.description);
              setInstructions(module.description || option.description);

              if (
                data.configuration &&
                typeof data.configuration === "object"
              ) {
                setConfiguration(cloneConfig(data.configuration));
              } else {
                setConfiguration(cloneConfig(module.defaultConfig || {}));
              }
            }

            if (data.configuration && typeof data.configuration === "object") {
              setConfiguration(cloneConfig(data.configuration));
            } else {
              setConfiguration(cloneConfig(module.defaultConfig || {}));
            }
          }
        } else if (
          data.configuration &&
          typeof data.configuration === "object"
        ) {
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

      if (!Array.isArray(questions)) {
        return previous;
      }

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

      if (!Array.isArray(questions)) {
        return previous;
      }

      questions.splice(questionIndex, 1);

      return next;
    });
  };

  const validateMemoryQuestions = () => {
    if (moduleId !== "Memory2") {
      return true;
    }

    const targets = Array.isArray(configuration.targets)
      ? configuration.targets
      : [];

    for (let targetIndex = 0; targetIndex < targets.length; targetIndex += 1) {
      const target = targets[targetIndex];

      const questions =
        target && Array.isArray(target.questions) ? target.questions : [];

      for (
        let questionIndex = 0;
        questionIndex < questions.length;
        questionIndex += 1
      ) {
        const question = questions[questionIndex];

        if (!question || typeof question !== "object") {
          alert(
            `Target ${targetIndex + 1}, Question ${
              questionIndex + 1
            } is invalid.`,
          );

          return false;
        }

        if (
          typeof question.questionText !== "string" ||
          !question.questionText.trim()
        ) {
          alert(
            `Please enter the question text for Target ${
              targetIndex + 1
            }, Question ${questionIndex + 1}.`,
          );

          return false;
        }

        if (
          typeof question.questionType !== "string" ||
          !question.questionType.trim()
        ) {
          alert(
            `Please select a question type for Target ${
              targetIndex + 1
            }, Question ${questionIndex + 1}.`,
          );

          return false;
        }

        if (
          typeof question.correctAnswer !== "string" ||
          !question.correctAnswer.trim()
        ) {
          alert(
            `Please enter the correct answer for Target ${
              targetIndex + 1
            }, Question ${questionIndex + 1}.`,
          );

          return false;
        }

        if (question.questionType === "MultipleChoice") {
          const options = Array.isArray(question.multipleChoiceOptions)
            ? question.multipleChoiceOptions
            : [];

          if (options.length === 0) {
            alert(
              `Please add at least one multiple choice option for Target ${
                targetIndex + 1
              }, Question ${questionIndex + 1}.`,
            );

            return false;
          }

          const hasEmptyOption = options.some(
            (option) => typeof option !== "string" || !option.trim(),
          );

          if (hasEmptyOption) {
            alert(
              `Please fill in all multiple choice options for Target ${
                targetIndex + 1
              }, Question ${questionIndex + 1}.`,
            );

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

      let newTarget;

      if (targets.length > 0) {
        newTarget = cloneConfig(targets[targets.length - 1]);

        if (Object.prototype.hasOwnProperty.call(newTarget, "objectName")) {
          newTarget.objectName = "";
        }

        if (Array.isArray(newTarget.questions)) {
          newTarget.questions = newTarget.questions.map(() => ({
            questionText: "",
            questionType: "MultipleChoice",
            multipleChoiceOptions: ["", ""],
            correctAnswer: "",
            enabled: true,
          }));

          if (newTarget.questions.length === 0) {
            newTarget.questions = [
              {
                questionText: "",
                questionType: "MultipleChoice",
                multipleChoiceOptions: ["", ""],
                correctAnswer: "",
                enabled: true,
              },
            ];
          }
        }
      } else {
        newTarget = {
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
      }

      targets.push(newTarget);

      next.targets = targets;

      return next;
    });

    setNewTargetIndex(
      Array.isArray(configuration.targets) ? configuration.targets.length : 0,
    );
  };

  const renderMemoryQuestions = (questions, path) => {
    return (
      <>
        <div className="md:col-span-2 flex justify-end">
          <button
            type="button"
            disabled={saving}
            onClick={() => addMemoryQuestion(path)}
            className="
              flex
              items-center
              gap-1.5
              rounded-lg
              border
              border-gray-300
              px-3
              py-1.5
              text-xs
              font-medium
              text-gray-700
              transition
              hover:bg-gray-50
              disabled:cursor-not-allowed
              disabled:opacity-50
            "
          >
            <Plus size={14} />
            Add Question
          </button>
        </div>

        {questions.length === 0 ? (
          <div className="md:col-span-2">
            <div className="rounded-lg border border-dashed border-gray-300 bg-white p-6 text-center">
              <p className="text-sm text-gray-500">
                No questions are configured.
              </p>
            </div>
          </div>
        ) : (
          <div className="md:col-span-2 space-y-4">
            {questions.map((question, index) => (
              <div
                key={`${path.join(".")}-${index}`}
                className="
                  overflow-hidden
                  rounded-xl
                  border
                  border-gray-200
                  bg-white
                  shadow-sm
                "
              >
                <div
                  className="
                    flex
                    items-center
                    justify-between
                    gap-3
                    border-b
                    border-gray-200
                    bg-gray-50
                    px-4
                    py-3
                  "
                >
                  <div className="flex items-center gap-3">
                    <div
                      className="
                        flex
                        h-8
                        w-8
                        items-center
                        justify-center
                        rounded-lg
                        bg-indigo-100
                        text-sm
                        font-semibold
                        text-indigo-700
                      "
                    >
                      {index + 1}
                    </div>

                    <div>
                      <p className="text-sm font-semibold text-gray-900">
                        Question {index + 1}
                      </p>
                    </div>
                  </div>

                  <button
                    type="button"
                    disabled={saving}
                    onClick={() => removeMemoryQuestion(path, index)}
                    className="
                      rounded-lg
                      px-2.5
                      py-1.5
                      text-xs
                      font-medium
                      text-red-600
                      transition
                      hover:bg-red-50
                      disabled:cursor-not-allowed
                      disabled:opacity-50
                    "
                  >
                    Remove
                  </button>
                </div>

                <div className="grid grid-cols-1 gap-4 p-4 md:grid-cols-2">
                  {question &&
                  typeof question === "object" &&
                  !Array.isArray(question) ? (
                    [
                      ...QUESTION_FIELD_ORDER.filter((questionKey) =>
                        Object.prototype.hasOwnProperty.call(
                          question,
                          questionKey,
                        ),
                      ),
                      ...Object.keys(question).filter(
                        (questionKey) =>
                          !QUESTION_FIELD_ORDER.includes(questionKey),
                      ),
                    ].map((questionKey) => {
                      const value = question[questionKey];
                      const questionPath = [...path, index, questionKey];

                      if (questionKey === "questionText") {
                        return (
                          <div
                            key={questionPath.join(".")}
                            className="md:col-span-2"
                          >
                            <label className="mb-1.5 block text-sm font-medium text-gray-700">
                              Question Text
                            </label>

                            <textarea
                              value={value ?? ""}
                              onChange={(e) =>
                                updateConfigValue(questionPath, e.target.value)
                              }
                              rows={3}
                              placeholder="Enter the question..."
                              className="
                                w-full
                                resize-none
                                rounded-lg
                                border
                                border-gray-300
                                bg-white
                                px-3
                                py-2.5
                                text-sm
                                outline-none
                                transition
                                focus:border-indigo-500
                                focus:ring-2
                                focus:ring-indigo-500/20
                              "
                            />
                          </div>
                        );
                      }

                      if (questionKey === "questionType") {
                        return (
                          <div key={questionPath.join(".")}>
                            <label className="mb-1.5 block text-sm font-medium text-gray-700">
                              Question Type
                            </label>

                            <select
                              value={
                                value === "YesNo" ? "YesNo" : "MultipleChoice"
                              }
                              onChange={(e) => {
                                const newType = e.target.value;

                                updateConfigValue(questionPath, newType);

                                const optionsPath = [
                                  ...path,
                                  index,
                                  "multipleChoiceOptions",
                                ];

                                if (newType === "MultipleChoice") {
                                  if (
                                    !Array.isArray(
                                      question.multipleChoiceOptions,
                                    )
                                  ) {
                                    updateConfigValue(optionsPath, ["", ""]);
                                  }
                                } else {
                                  updateConfigValue(optionsPath, []);
                                }
                              }}
                              className="
                                w-full
                                rounded-lg
                                border
                                border-gray-300
                                bg-white
                                px-3
                                py-2.5
                                text-sm
                                outline-none
                                transition
                                focus:border-indigo-500
                                focus:ring-2
                                focus:ring-indigo-500/20
                              "
                            >
                              <option value="MultipleChoice">
                                Multiple Choice
                              </option>

                              <option value="YesNo">Yes / No</option>
                            </select>
                          </div>
                        );
                      }

                      if (questionKey === "multipleChoiceOptions") {
                        if (question?.questionType !== "MultipleChoice") {
                          return null;
                        }
                        const options = Array.isArray(value) ? value : [];

                        const addOption = () => {
                          updateConfigValue(questionPath, [...options, ""]);
                        };

                        const updateOption = (optionIndex, newValue) => {
                          const nextOptions = [...options];

                          nextOptions[optionIndex] = newValue;

                          updateConfigValue(questionPath, nextOptions);
                        };

                        const removeOption = (optionIndex) => {
                          const nextOptions = options.filter(
                            (_, i) => i !== optionIndex,
                          );

                          updateConfigValue(questionPath, nextOptions);
                        };

                        return (
                          <div
                            key={questionPath.join(".")}
                            className="md:col-span-2"
                          >
                            <div className="mb-2 flex items-center justify-between">
                              <div>
                                <label className="block text-sm font-medium text-gray-700">
                                  Multiple Choice Options
                                </label>
                              </div>

                              <button
                                type="button"
                                onClick={addOption}
                                className="
                                  flex
                                  items-center
                                  gap-1.5
                                  rounded-lg
                                  border
                                  border-gray-300
                                  px-3
                                  py-1.5
                                  text-xs
                                  font-medium
                                  text-gray-700
                                  transition
                                  hover:bg-gray-50
                                "
                              >
                                <Plus size={14} />
                                Add Option
                              </button>
                            </div>

                            <div className="space-y-2">
                              {options.map((option, optionIndex) => (
                                <div
                                  key={`${questionPath.join(
                                    ".",
                                  )}-${optionIndex}`}
                                  className="flex items-center gap-2"
                                >
                                  <div
                                    className="
                                      flex
                                      h-9
                                      w-9
                                      shrink-0
                                      items-center
                                      justify-center
                                      rounded-lg
                                      bg-indigo-50
                                      text-sm
                                      font-medium
                                      text-indigo-700
                                    "
                                  >
                                    {optionIndex + 1}
                                  </div>

                                  <input
                                    type="text"
                                    value={option ?? ""}
                                    onChange={(e) =>
                                      updateOption(optionIndex, e.target.value)
                                    }
                                    placeholder={`Option ${optionIndex + 1}`}
                                    className="
                                      w-full
                                      rounded-lg
                                      border
                                      border-gray-300
                                      bg-white
                                      px-3
                                      py-2
                                      text-sm
                                      outline-none
                                      transition
                                      focus:border-indigo-500
                                      focus:ring-2
                                      focus:ring-indigo-500/20
                                    "
                                  />

                                  <button
                                    type="button"
                                    onClick={() => removeOption(optionIndex)}
                                    className="
                                      flex
                                      h-9
                                      w-9
                                      shrink-0
                                      items-center
                                      justify-center
                                      rounded-lg
                                      border
                                      border-gray-300
                                      text-gray-500
                                      transition
                                      hover:border-red-300
                                      hover:bg-red-50
                                      hover:text-red-600
                                    "
                                  >
                                    ×
                                  </button>
                                </div>
                              ))}
                            </div>
                          </div>
                        );
                      }

                      if (questionKey === "correctAnswer") {
                        return (
                          <div key={questionPath.join(".")}>
                            <label className="mb-1.5 block text-sm font-medium text-gray-700">
                              Correct Answer
                            </label>

                            <input
                              type="text"
                              value={value ?? ""}
                              onChange={(e) =>
                                updateConfigValue(questionPath, e.target.value)
                              }
                              className="
                                w-full
                                rounded-lg
                                border
                                border-gray-300
                                bg-white
                                px-3
                                py-2.5
                                text-sm
                                outline-none
                                transition
                                focus:border-indigo-500
                                focus:ring-2
                                focus:ring-indigo-500/20
                              "
                            />
                          </div>
                        );
                      }

                      if (questionKey === "enabled") {
                        return (
                          <div
                            key={questionPath.join(".")}
                            className="md:col-span-2"
                          >
                            <label className="mb-2 block text-sm font-medium text-gray-700">
                              Question Enabled
                            </label>

                            <div className="flex items-center gap-6">
                              <label className="flex cursor-pointer items-center gap-2 text-sm">
                                <input
                                  type="radio"
                                  name={`question-${path.join("-")}-${index}`}
                                  checked={value === true}
                                  onChange={() =>
                                    updateConfigValue(questionPath, true)
                                  }
                                  className="h-4 w-4 accent-indigo-600"
                                />
                                Yes
                              </label>

                              <label className="flex cursor-pointer items-center gap-2 text-sm">
                                <input
                                  type="radio"
                                  name={`question-${path.join("-")}-${index}`}
                                  checked={value === false}
                                  onChange={() =>
                                    updateConfigValue(questionPath, false)
                                  }
                                  className="h-4 w-4 accent-indigo-600"
                                />
                                No
                              </label>
                            </div>
                          </div>
                        );
                      }

                      return (
                        <div key={questionPath.join(".")}>
                          <label className="mb-1.5 block text-sm font-medium text-gray-700">
                            {formatConfigLabel(questionKey)}
                          </label>

                          <input
                            type="text"
                            value={
                              typeof value === "string"
                                ? value
                                : String(value ?? "")
                            }
                            onChange={(e) =>
                              updateConfigValue(questionPath, e.target.value)
                            }
                            className="
                              w-full
                              rounded-lg
                              border
                              border-gray-300
                              bg-white
                              px-3
                              py-2.5
                              text-sm
                              outline-none
                              transition
                              focus:border-indigo-500
                              focus:ring-2
                              focus:ring-indigo-500/20
                            "
                          />
                        </div>
                      );
                    })
                  ) : (
                    <p className="text-sm text-gray-500">
                      Invalid question configuration.
                    </p>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </>
    );
  };

  const renderTargetField = (value, path, key) => {
    if (key === "questions" && Array.isArray(value)) {
      return renderMemoryQuestions(value, path);
    }

    if (key === "objectName") {
      return null;
    }

    if (typeof value === "boolean") {
      return (
        <div
          key={path.join(".")}
          className="rounded-lg border border-gray-200 bg-gray-50 p-3"
        >
          <label className="mb-2 block text-sm font-medium text-gray-700">
            {formatConfigLabel(key)}
          </label>

          <div className="flex items-center gap-5">
            <label className="flex cursor-pointer items-center gap-2 text-sm">
              <input
                type="radio"
                name={`target-${path.join("-")}`}
                checked={value === true}
                onChange={() => updateConfigValue(path, true)}
                className="h-4 w-4 accent-indigo-600"
              />
              Yes
            </label>

            <label className="flex cursor-pointer items-center gap-2 text-sm">
              <input
                type="radio"
                name={`target-${path.join("-")}`}
                checked={value === false}
                onChange={() => updateConfigValue(path, false)}
                className="h-4 w-4 accent-indigo-600"
              />
              No
            </label>
          </div>
        </div>
      );
    }

    if (typeof value === "number") {
      return (
        <div key={path.join(".")}>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">
            {formatConfigLabel(key)}
          </label>

          <input
            type="number"
            step="any"
            value={value}
            onChange={(e) => {
              const nextValue = e.target.value;

              updateConfigValue(
                path,
                nextValue === "" ? "" : Number(nextValue),
              );
            }}
            className="
            w-full
            rounded-lg
            border
            border-gray-300
            bg-white
            px-3
            py-2.5
            text-sm
            outline-none
            transition
            focus:border-indigo-500
            focus:ring-2
            focus:ring-indigo-500/20
          "
          />
        </div>
      );
    }

    if (typeof value === "string") {
      const longText = isLongTextKey(key);

      return (
        <div key={path.join(".")} className={longText ? "md:col-span-2" : ""}>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">
            {formatConfigLabel(key)}
          </label>

          {longText ? (
            <textarea
              value={value}
              onChange={(e) => updateConfigValue(path, e.target.value)}
              rows={3}
              className="
              w-full
              resize-none
              rounded-lg
              border
              border-gray-300
              bg-white
              px-3
              py-2.5
              text-sm
              outline-none
              transition
              focus:border-indigo-500
              focus:ring-2
              focus:ring-indigo-500/20
            "
            />
          ) : (
            <input
              type="text"
              value={value}
              onChange={(e) => updateConfigValue(path, e.target.value)}
              className="
              w-full
              rounded-lg
              border
              border-gray-300
              bg-white
              px-3
              py-2.5
              text-sm
              outline-none
              transition
              focus:border-indigo-500
              focus:ring-2
              focus:ring-indigo-500/20
            "
            />
          )}
        </div>
      );
    }

    if (Array.isArray(value) || (typeof value === "object" && value !== null)) {
      return (
        <div
          key={path.join(".")}
          className="md:col-span-2 rounded-lg border border-gray-200 bg-gray-50 p-3"
        >
          <label className="mb-2 block text-sm font-medium text-gray-700">
            {formatConfigLabel(key)}
          </label>

          {Array.isArray(value) ? (
            <div className="space-y-2">
              {value.map((item, index) => (
                <div
                  key={`${path.join(".")}-${index}`}
                  className="rounded-md bg-white p-2 text-sm text-gray-600"
                >
                  {typeof item === "object"
                    ? JSON.stringify(item, null, 2)
                    : String(item)}
                </div>
              ))}
            </div>
          ) : (
            <pre className="overflow-x-auto whitespace-pre-wrap text-xs text-gray-600">
              {JSON.stringify(value, null, 2)}
            </pre>
          )}
        </div>
      );
    }

    return (
      <div key={path.join(".")}>
        <label className="mb-1.5 block text-sm font-medium text-gray-700">
          {formatConfigLabel(key)}
        </label>

        <input
          type="text"
          value={value ?? ""}
          onChange={(e) => updateConfigValue(path, e.target.value)}
          className="
          w-full
          rounded-lg
          border
          border-gray-300
          bg-white
          px-3
          py-2.5
          text-sm
          outline-none
          transition
          focus:border-indigo-500
          focus:ring-2
          focus:ring-indigo-500/20
        "
        />
      </div>
    );
  };

  const scrollToTarget = (index) => {
    const target = targetRefs.current[index];

    if (!target) return;

    setActiveTargetIndex(index);

    target.scrollIntoView({
      behavior: "smooth",
      block: "center",
    });
  };

  const removeMemoryTarget = (index) => {
    setConfiguration((previous) => {
      const targets = Array.isArray(previous.targets) ? previous.targets : [];

      return {
        ...previous,
        targets: targets.filter((_, targetIndex) => targetIndex !== index),
      };
    });

    setActiveTargetIndex((previous) => {
      if (previous === null) return null;

      if (previous === index) {
        return null;
      }

      if (previous > index) {
        return previous - 1;
      }

      return previous;
    });
  };

  const renderMemoryTargets = (targets, path) => {
    return (
      <div className="rounded-xl border border-gray-200 bg-gray-50 p-4">
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-base font-semibold text-gray-900">
              Memory Targets
            </h3>
          </div>

          <div className="flex items-center gap-2">
            <div className="rounded-full bg-indigo-100 px-3 py-1 text-xs font-medium text-indigo-700">
              {targets.length} {targets.length === 1 ? "Target" : "Targets"}
            </div>
            {/** 
            <button
              type="button"
              disabled={saving}
              onClick={addMemoryTarget}
              className="
                flex
                items-center
                gap-1.5
                rounded-lg
                border
                border-gray-300
                bg-white
                px-3
                py-1.5
                text-xs
                font-medium
                text-gray-700
                transition
                hover:bg-gray-50
                disabled:cursor-not-allowed
                disabled:opacity-50
              "
            >
              <Plus size={14} />
              Add Target
            </button>
            */}
          </div>
        </div>

        <div
          className="
            sticky
            top-24
            z-10
            mb-4
            rounded-xl
            border
            border-gray-200
            bg-white/95
            p-3
            shadow-sm
            backdrop-blur
          "
        >
          <div className="mb-2 flex items-center justify-between">
            <p className="text-xs font-semibold uppercase tracking-wide text-gray-500">
              Jump to Target
            </p>

            <span className="text-xs text-gray-400">
              {targets.length} total
            </span>
          </div>

          {targets.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {targets.map((target, index) => (
                <button
                  key={index}
                  type="button"
                  onClick={() => scrollToTarget(index)}
                  className={`
                    flex
                    h-9
                    min-w-9
                    items-center
                    justify-center
                    rounded-lg
                    border
                    px-2.5
                    text-sm
                    font-medium
                    transition
                    ${
                      activeTargetIndex === index
                        ? "border-indigo-700 bg-indigo-700 text-white shadow-sm"
                        : "border-gray-200 bg-gray-50 text-gray-600 hover:border-indigo-300 hover:bg-indigo-50 hover:text-indigo-700"
                    }
                  `}
                >
                  {index + 1}
                </button>
              ))}
            </div>
          ) : (
            <p className="text-sm text-gray-400">
              No targets are currently configured.
            </p>
          )}
        </div>

        {targets.length === 0 ? (
          <div className="rounded-lg border border-dashed border-gray-300 bg-white p-8 text-center">
            <p className="text-sm font-medium text-gray-700">
              No memory targets are configured.
            </p>

            <p className="mt-1 text-xs text-gray-500">
              Targets are defined by the experiment module configuration.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {targets.map((target, index) => {
              const targetKeys = getOrderedTargetKeys(target);

              return (
                <div
                  key={`${path.join(".")}-${index}`}
                  ref={(element) => {
                    targetRefs.current[index] = element;
                  }}
                  className={`
                    overflow-hidden
                    rounded-xl
                    border
                    bg-white
                    shadow-sm
                    transition-all
                    duration-300
                    ${
                      activeTargetIndex === index
                        ? "border-indigo-300"
                        : "border-gray-200"
                    }
                  `}
                >
                  <div
                    className="
                    flex
                    items-center
                    justify-between
                    border-b
                    border-gray-200
                    bg-gray-50
                    px-4
                    py-3
                  "
                  >
                    <div className="flex items-center gap-3">
                      <div
                        className="
                        flex
                        h-8
                        w-8
                        items-center
                        justify-center
                        rounded-lg
                        bg-indigo-100
                        text-sm
                        font-semibold
                        text-indigo-700
                      "
                      >
                        {index + 1}
                      </div>

                      <div>
                        <p className="text-sm font-semibold text-gray-900">
                          Target {index + 1}
                          {target.objectName ? ` (${target.objectName})` : ""}
                        </p>
                      </div>
                    </div>

                    <button
                      type="button"
                      onClick={() => removeMemoryTarget(index)}
                      disabled={saving}
                      className="
                      rounded-lg
                      px-2.5
                      py-1.5
                      text-xs
                      font-medium
                      text-red-600
                      transition
                      hover:bg-red-50
                      disabled:cursor-not-allowed
                      disabled:opacity-50
                    "
                    >
                      Remove
                    </button>
                  </div>

                  <div className="grid grid-cols-1 gap-4 p-4 md:grid-cols-2">
                    {targetKeys.map((targetKey) =>
                      renderTargetField(
                        target[targetKey],
                        [...path, index, targetKey],
                        targetKey,
                      ),
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    );
  };

  const renderConfigValue = (value, path, key, ancestors = []) => {
    if (
      value !== null &&
      typeof value === "object" &&
      ancestors.includes(value)
    ) {
      return (
        <div
          key={path.join(".")}
          className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-600"
        >
          Unable to display this configuration value because it contains a
          circular reference.
        </div>
      );
    }

    const nextAncestors =
      value !== null && typeof value === "object"
        ? [...ancestors, value]
        : ancestors;

    if (Array.isArray(value)) {
      if (moduleId === "Memory2" && key === "targets") {
        return renderMemoryTargets(value, path);
      }

      return (
        <div
          key={path.join(".")}
          className="rounded-lg border border-gray-200 bg-gray-50 p-3"
        >
          <div className="mb-3 text-sm font-medium text-gray-700">
            {formatConfigLabel(key)}
          </div>

          {value.length === 0 ? (
            <p className="text-sm text-gray-500">No items configured.</p>
          ) : (
            <div className="flex flex-col gap-3">
              {value.map((item, index) => (
                <div
                  key={`${path.join(".")}-${index}`}
                  className="rounded-lg border border-gray-200 bg-white p-3"
                >
                  {renderConfigValue(
                    item,
                    [...path, index],
                    `${key} ${index + 1}`,
                    nextAncestors,
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      );
    }

    if (typeof value === "object" && value !== null) {
      return (
        <div
          key={path.join(".")}
          className="mt-2 rounded-lg border border-gray-200 bg-gray-50 p-3"
        >
          <div className="mb-3 text-sm font-medium text-gray-700">
            {formatConfigLabel(key)}
          </div>

          <div className="flex flex-col gap-3">
            {Object.entries(value).map(([childKey, childValue]) => (
              <div key={[...path, childKey].join(".")}>
                {renderConfigValue(
                  childValue,
                  [...path, childKey],
                  childKey,
                  nextAncestors,
                )}
              </div>
            ))}
          </div>
        </div>
      );
    }

    if (typeof value === "boolean") {
      return (
        <div key={path.join(".")} className="flex flex-wrap items-center gap-4">
          <span className="text-sm font-medium">{formatConfigLabel(key)}</span>

          <label className="flex items-center gap-2 text-sm">
            <input
              type="radio"
              name={`config-${path.join("-")}`}
              checked={value === true}
              onChange={() => updateConfigValue(path, true)}
              className="accent-indigo-700"
            />
            Yes
          </label>

          <label className="flex items-center gap-2 text-sm">
            <input
              type="radio"
              name={`config-${path.join("-")}`}
              checked={value === false}
              onChange={() => updateConfigValue(path, false)}
              className="accent-indigo-700"
            />
            No
          </label>
        </div>
      );
    }

    if (typeof value === "number") {
      return (
        <div key={path.join(".")}>
          <label className="mb-1 block text-sm font-medium">
            {formatConfigLabel(key)}
          </label>

          <input
            type="number"
            step="any"
            value={value}
            onChange={(e) => {
              const nextValue = e.target.value;

              updateConfigValue(
                path,
                nextValue === "" ? "" : Number(nextValue),
              );
            }}
            className="
              w-full
              rounded-lg
              border
              border-gray-300
              bg-gray-200
              px-3
              py-2
              outline-none
              focus:ring-2
              focus:ring-indigo-500
            "
          />
        </div>
      );
    }

    if (typeof value === "string") {
      const longText = isLongTextKey(key);

      return (
        <div key={path.join(".")}>
          <label className="mb-1 block text-sm font-medium">
            {formatConfigLabel(key)}
          </label>

          {longText ? (
            <textarea
              value={value}
              onChange={(e) => updateConfigValue(path, e.target.value)}
              rows={3}
              className="
                w-full
                resize-none
                rounded-lg
                border
                border-gray-300
                bg-gray-200
                px-3
                py-2
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />
          ) : (
            <input
              type="text"
              value={value}
              onChange={(e) => updateConfigValue(path, e.target.value)}
              className="
                w-full
                rounded-lg
                border
                border-gray-300
                bg-gray-200
                px-3
                py-2
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />
          )}
        </div>
      );
    }

    return (
      <div key={path.join(".")}>
        <label className="mb-1 block text-sm font-medium">
          {formatConfigLabel(key)}
        </label>

        <input
          type="text"
          value={value ?? ""}
          onChange={(e) => updateConfigValue(path, e.target.value)}
          className="
            w-full
            rounded-lg
            border
            border-gray-300
            bg-gray-200
            px-3
            py-2
            outline-none
            focus:ring-2
            focus:ring-indigo-500
          "
        />
      </div>
    );
  };

  const saveExperiment = async () => {
    if (!experimentName.trim()) {
      alert("Please enter an experiment name.");
      return;
    }

    if (!environment || !moduleId) {
      alert("Please select a VR environment.");
      return;
    }

    if (!validateMemoryQuestions()) {
      return;
    }

    try {
      setSaving(true);

      const experimentData = {
        experimentName: experimentName.trim(),
        instructions: instructions.trim(),
        blockId,
        environment,
        moduleId,
        moduleName,
        sceneId,
        moduleDescription,
        configuration: cloneConfig(configuration),
        groupIds: selectedGroups,
        allowStudentExperiments,
        updatedAt: serverTimestamp(),
      };

      if (isEditing) {
        const experimentRef = doc(db, "experiment", experimentId);

        await updateDoc(experimentRef, experimentData);

        alert("Experiment updated and published successfully!");
      } else {
        await addDoc(collection(db, "experiment"), {
          ...experimentData,
          createdAt: serverTimestamp(),
        });

        alert("Experiment published successfully!");
      }

      navigate(-1);
    } catch (error) {
      console.error("Error saving experiment:", error);

      alert("Unable to save the experiment. Please try again.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="font-google flex min-h-screen items-center justify-center">
        <p>Loading experiment...</p>
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
            transition
            hover:text-black
          "
        >
          <ArrowLeft size={20} />

          <span>Back to Experiments</span>
        </button>

        <div className="mx-auto max-w-5xl px-6 pb-12">
          <h1 className="mt-5 mb-6 text-3xl font-medium">
            {isEditing ? "Edit Experiment" : "New Experiment"}
          </h1>

          <section
            className="
              mb-5
              rounded-lg
              border
              border-gray-300
              p-4
            "
          >
            <h2 className="mb-3 text-lg font-medium">VR Environment</h2>

            {moduleLoading ? (
              <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
                <p className="text-sm text-gray-500">
                  Loading experiment environments...
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
                {ENVIRONMENT_OPTIONS.map((option) => {
                  const selected =
                    environment === option.name || moduleId === option.moduleId;

                  return (
                    <button
                      key={option.moduleId}
                      type="button"
                      disabled={saving}
                      onClick={() => selectEnvironment(option)}
                      className={`
                          rounded-lg
                          border
                          px-3
                          py-2
                          text-left
                          transition
                          disabled:cursor-not-allowed
                          disabled:opacity-60
                          ${
                            selected
                              ? "border-indigo-700 bg-gray-200 ring-1 ring-indigo-700"
                              : "border-gray-300 hover:bg-gray-50"
                          }
                        `}
                    >
                      <p className="font-medium">{option.name}</p>

                      <p className="text-sm text-gray-500">
                        {option.description}
                      </p>
                    </button>
                  );
                })}
              </div>
            )}
          </section>

          <section
            className="
              mb-5
              rounded-lg
              border
              border-gray-300
              p-4
            "
          >
            <h2 className="mb-3 text-lg font-medium">Basic Information</h2>

            <label className="mb-1 block text-sm">Experiment Name</label>

            <input
              type="text"
              value={experimentName}
              onChange={(e) => setExperimentName(e.target.value)}
              placeholder="e.g., Inattentional Blindness"
              className="
                mb-3
                w-full
                rounded-lg
                border
                border-gray-300
                bg-gray-200
                px-3
                py-2
                outline-none
                focus:ring-2
                focus:ring-indigo-500
              "
            />

            <label className="mb-1 block text-sm">
              Description / Instructions
            </label>

            <textarea
              value={instructions}
              onChange={(e) => setInstructions(e.target.value)}
              placeholder="Describe the tasks needed to perform this experiment"
              rows={4}
              className="
                mb-3
                w-full
                resize-none
                rounded-lg
                border
                border-gray-300
                bg-gray-200
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
              mb-5
              rounded-lg
              border
              border-gray-300
              p-4
            "
          >
            <div className="mb-3">
              <h2 className="text-lg font-medium">Configuration</h2>
            </div>

            {!moduleId ? (
              <div className="rounded-lg bg-gray-50 p-4 text-sm text-gray-500">
                Select a VR environment to load its default configuration.
              </div>
            ) : Object.keys(configuration).length === 0 ? (
              <div className="rounded-lg bg-gray-50 p-4 text-sm text-gray-500">
                This experiment module does not have a default configuration.
              </div>
            ) : (
              <div className="flex flex-col gap-4">
                {getOrderedConfigurationKeys().map((key) =>
                  renderConfigValue(configuration[key], [key], key),
                )}
              </div>
            )}
          </section>

          <section
            className="
              mb-5
              rounded-lg
              border
              border-gray-300
              p-4
            "
          >
            <h2 className="mb-3 text-lg font-medium">Groups</h2>

            {groups.length === 0 ? (
              <p className="text-gray-500">
                No groups have been created for this course.
              </p>
            ) : (
              <div className="flex flex-col gap-3">
                {groups.map((group) => (
                  <label
                    key={group.id}
                    className="
                      flex
                      cursor-pointer
                      items-center
                      gap-3
                    "
                  >
                    <input
                      type="checkbox"
                      checked={selectedGroups.includes(group.id)}
                      onChange={() => toggleGroup(group.id)}
                      className="h-4 w-4 accent-indigo-600"
                    />

                    <span>
                      {group.groupName || group.name || "Unnamed Group"}
                    </span>
                  </label>
                ))}
              </div>
            )}
          </section>

          <div className="mb-6 flex flex-wrap items-center gap-3">
            <span>
              Allow Students to create an experiment for this activity?
            </span>

            <label className="flex items-center gap-1">
              <input
                type="radio"
                name="studentExperimentPermission"
                checked={allowStudentExperiments === true}
                onChange={() => setAllowStudentExperiments(true)}
                className="accent-indigo-600"
              />
              Yes
            </label>

            <label className="flex items-center gap-1">
              <input
                type="radio"
                name="studentExperimentPermission"
                checked={allowStudentExperiments === false}
                onChange={() => setAllowStudentExperiments(false)}
                className="accent-indigo-600"
              />
              No
            </label>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={saving || moduleLoading}
              onClick={saveExperiment}
              className="
                flex
                items-center
                gap-2
                rounded-lg
                bg-indigo-800
                px-4
                py-2
                text-white
                transition
                hover:bg-indigo-700
                disabled:bg-gray-400
              "
            >
              <Plus size={18} />

              {saving
                ? "Saving..."
                : isEditing
                  ? "Update Experiment"
                  : "Publish Experiment"}
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}
