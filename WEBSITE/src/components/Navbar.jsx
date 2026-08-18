import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";

import { auth, db } from "../config/firebase-config";

import { doc, getDoc } from "firebase/firestore";

import { signOut } from "firebase/auth";

import { UserCircle, Settings, LogOut } from "lucide-react";

import { useAuth } from "../context/AuthContext";

export default function Navbar() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [userData, setUserData] = useState(null);
  const [showProfile, setShowProfile] = useState(false);
  const profileRef = useRef(null);

  useEffect(() => {
    const getUserData = async () => {
      if (!user) return;
      try {
        const userRef = doc(db, "user", user.uid);

        const userSnap = await getDoc(userRef);

        if (userSnap.exists()) {
          setUserData(userSnap.data());
        }
      } catch (error) {
        console.error("Error getting user data:", error);
      }
    };
    getUserData();
  }, [user]);

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (profileRef.current && !profileRef.current.contains(event.target)) {
        setShowProfile(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const handleLogout = async () => {
    try {
      await signOut(auth);
      navigate("/login");
    } catch (error) {
      console.error("Logout error:", error);
    }
  };

  return (
    <nav
      className="
        font-google
        flex
        justify-between
        items-center
        fixed
        top-0
        w-full
        h-[68px]
        bg-indigo-800
        px-4
        z-50
      "
    >
      <h1
        className="
          text-white
          text-4xl
          cursor-pointer
        "
        onClick={() => {
          if (userData?.role === "student") {
            navigate("/student");
          } else if (userData?.role === "instructor") {
            navigate("/instructor");
          }
        }}
      >
        SLUBT Labs
      </h1>
      <div
        ref={profileRef}
        className="
          relative
          h-full
          flex
          items-center
        "
      >
        <button
          onClick={() => setShowProfile(!showProfile)}
          className="
            flex
            items-center
            gap-3
            text-white
            hover:opacity-90
            transition
          "
        >
          <span className="text-2xl">
            {userData ? `${userData.firstName}` : "Loading..."}
          </span>
          <div
            className="
              w-14
              h-14
              rounded-full
              bg-gray-300
              border-4
              border-white
              flex
              items-center
              justify-center
              overflow-hidden
            "
          >
            <UserCircle size={48} strokeWidth={1.5} className="text-gray-600" />
          </div>
        </button>

        {showProfile && (
          <div
            className="
              absolute
              right-0
              top-[68px]
              w-[250px]
              bg-white
              border
              border-gray-300
              rounded-lg
              shadow-lg
              p-4
              text-black
            "
          >
            <div
              className="
                text-center
                pb-3
                border-b
                border-gray-300
              "
            >
              <h2 className="text-xl">
                {userData
                  ? `${userData.firstName} ${userData.lastName}`
                  : "Loading..."}
              </h2>

              <p
                className="
                  text-gray-600
                  text-base
                  mt-1
                "
              >
                {userData?.email || user?.email || "Loading..."}
              </p>
            </div>
            <button
              onClick={() => {
                setShowProfile(false);
                navigate("/account");
              }}
              className="
                w-full
                flex
                items-center
                gap-3
                py-3
                text-left
                text-lg
                hover:bg-gray-100
                rounded-md
                transition
              "
            >
              <UserCircle size={22} strokeWidth={2} />

              <span>My Account</span>
            </button>

            <button
              onClick={() => {
                setShowProfile(false);

                navigate("/settings");
              }}
              className="
                w-full
                flex
                items-center
                gap-3
                py-3
                text-left
                text-lg
                hover:bg-gray-100
                rounded-md
                transition
              "
            >
              <Settings size={22} strokeWidth={2} />

              <span>Settings</span>
            </button>
            <div
              className="
                border-t
                border-gray-300
                my-1
              "
            />
            <button
              onClick={handleLogout}
              className="
                w-full
                flex
                items-center
                gap-3
                py-3
                text-left
                text-lg
                text-red-500
                hover:bg-red-50
                rounded-md
                transition
              "
            >
              <LogOut size={22} strokeWidth={2} />

              <span>Sign Out</span>
            </button>
          </div>
        )}
      </div>
    </nav>
  );
}
