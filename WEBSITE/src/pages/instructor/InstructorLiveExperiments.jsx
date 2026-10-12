import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";

import { db } from "../../config/firebase-config";

import { collection, getDocs, query, where } from "firebase/firestore";

import { ArrowLeft, Box } from "lucide-react";

export default function InstructorLiveExperiments() {
  const navigate = useNavigate();
  const { blockId } = useParams();

  const [groups, setGroups] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    document.title = "Instructor - View Live Experiments";
  }, []);

  useEffect(() => {
    const getGroups = async () => {
      try {
        setLoading(true);
        setError("");

        const groupsRef = collection(db, "group");

        const groupQuery = query(groupsRef, where("blockId", "==", blockId));

        const groupSnapshot = await getDocs(groupQuery);

        const groupList = groupSnapshot.docs
          .map((groupDoc) => ({
            id: groupDoc.id,
            ...groupDoc.data(),
          }))
          .sort((a, b) =>
            (a.groupName || "").localeCompare(b.groupName || ""),
          );

        setGroups(groupList);
      } catch (error) {
        console.error("Error getting groups:", error);

        setError("Unable to load the live experiments.");
      } finally {
        setLoading(false);
      }
    };

    if (blockId) {
      getGroups();
    }
  }, [blockId]);

  if (loading) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p className="text-gray-500">Loading live experiments...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="font-google min-h-screen flex items-center justify-center">
        <p className="text-red-500">{error}</p>
      </div>
    );
  }

  return (
    <div className="font-google min-h-screen bg-white text-black">
      <main className="pt-20 px-6 pb-10 max-w-6xl mx-auto">
        <button
          onClick={() => navigate(`/instructor/course/${blockId}`)}
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

          <span className="text-lg">Back to Experiments</span>
        </button>

        {groups.length === 0 ? (
          <div className="py-16 text-center text-gray-500">
            No groups have been created for this course yet.
          </div>
        ) : (
          <div className="grid md:grid-cols-2 gap-x-8 gap-y-10">
            {groups.map((group) => (
              <div key={group.id}>
                <h2 className="text-lg font-medium mb-2">
                  {group.groupName || "Group"}
                </h2>

                <div
                  className="
                    bg-gray-200
                    rounded-lg
                    h-64
                    flex
                    flex-col
                    items-center
                    justify-center
                    text-gray-500
                  "
                >
                  <Box size={40} className="mb-3" />

                  <p>VR environment preview</p>

                  <p className="text-sm">Awaiting session start</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
