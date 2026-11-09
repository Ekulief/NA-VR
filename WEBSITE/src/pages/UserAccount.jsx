import { useEffect, useState } from "react";
import {
  EmailAuthProvider,
  reauthenticateWithCredential,
  updatePassword,
} from "firebase/auth";
import { useNavigate } from "react-router-dom";
import { auth, db } from "../config/firebase-config";
import { doc, getDoc } from "firebase/firestore";
import {
  ArrowLeft,
  Eye,
  EyeOff,
  LockKeyhole,
  CheckCircle,
  AlertCircle,
  UserCircle,
  Mail,
  ShieldCheck,
} from "lucide-react";

export default function UserAccount() {
  const navigate = useNavigate();

  const [userData, setUserData] = useState(null);
  const [loadingProfile, setLoadingProfile] = useState(true);
  const [profileError, setProfileError] = useState("");

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("");

  // Fetch the signed-in user's profile from Firestore.
  useEffect(() => {
    let isMounted = true;

    const fetchUserData = async () => {
      setLoadingProfile(true);
      setProfileError("");

      try {
        const currentUser = auth.currentUser;

        if (!currentUser) {
          if (isMounted) {
            setProfileError("You must be signed in to view your account.");
          }
          return;
        }

        const userRef = doc(db, "user", currentUser.uid);
        const userSnap = await getDoc(userRef);

        if (!isMounted) return;

        if (userSnap.exists()) {
          setUserData(userSnap.data());
        } else {
          setUserData({});
          setProfileError(
            "Your Firestore profile could not be found. Some information may be unavailable.",
          );
        }
      } catch (error) {
        console.error("Error fetching user profile:", error);

        if (isMounted) {
          setProfileError("Unable to load your profile information.");
        }
      } finally {
        if (isMounted) {
          setLoadingProfile(false);
        }
      }
    };

    fetchUserData();

    return () => {
      isMounted = false;
    };
  }, []);

  const currentUser = auth.currentUser;

  const fullName =
    [userData?.firstName, userData?.lastName].filter(Boolean).join(" ") ||
    "Name not available";

  const email = userData?.email || currentUser?.email || "Email not available";

  const role = userData?.role
    ? userData.role.charAt(0).toUpperCase() + userData.role.slice(1)
    : "Role not available";

  const handlePasswordChange = async (event) => {
    event.preventDefault();

    setMessage("");
    setMessageType("");

    const user = auth.currentUser;

    if (!user) {
      setMessage("You must be signed in to change your password.");
      setMessageType("error");
      return;
    }

    if (!user.email) {
      setMessage("Your account does not have an email address.");
      setMessageType("error");
      return;
    }

    if (!currentPassword || !newPassword || !confirmPassword) {
      setMessage("Please fill in all password fields.");
      setMessageType("error");
      return;
    }

    if (newPassword.length < 6) {
      setMessage("Your new password must be at least 6 characters long.");
      setMessageType("error");
      return;
    }

    if (newPassword === currentPassword) {
      setMessage(
        "Your new password must be different from your current password.",
      );
      setMessageType("error");
      return;
    }

    if (newPassword !== confirmPassword) {
      setMessage("The new password and confirmation password do not match.");
      setMessageType("error");
      return;
    }

    setLoading(true);

    try {
      const credential = EmailAuthProvider.credential(
        user.email,
        currentPassword,
      );

      await reauthenticateWithCredential(user, credential);
      await updatePassword(user, newPassword);

      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");

      setMessage("Your password has been changed successfully.");
      setMessageType("success");
    } catch (error) {
      console.error("Password change error:", error);

      switch (error.code) {
        case "auth/wrong-password":
        case "auth/invalid-credential":
          setMessage("Your current password is incorrect.");
          break;

        case "auth/weak-password":
          setMessage(
            "Your new password is too weak. Please choose a stronger password.",
          );
          break;

        case "auth/too-many-requests":
          setMessage("Too many attempts. Please try again later.");
          break;

        case "auth/network-request-failed":
          setMessage("A network error occurred. Please check your connection.");
          break;

        case "auth/requires-recent-login":
          setMessage("Please sign in again before changing your password.");
          break;

        default:
          setMessage(
            error.message ||
              "Unable to change your password. Please try again.",
          );
      }

      setMessageType("error");
    } finally {
      setLoading(false);
    }
  };

  const passwordField = (
    label,
    value,
    setValue,
    visible,
    setVisible,
    placeholder,
  ) => (
    <div>
      <label className="mb-2 block text-sm font-semibold text-gray-700">
        {label}
      </label>

      <div className="relative">
        <input
          type={visible ? "text" : "password"}
          value={value}
          onChange={(event) => setValue(event.target.value)}
          placeholder={placeholder}
          autoComplete="off"
          className="w-full rounded-lg border border-gray-300 bg-white px-4 py-3 pr-12 text-gray-800 outline-none transition focus:border-indigo-600 focus:ring-2 focus:ring-indigo-100"
          required
          disabled={loading}
        />

        <button
          type="button"
          onClick={() => setVisible(!visible)}
          className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-indigo-700"
          aria-label={visible ? "Hide password" : "Show password"}
        >
          {visible ? <EyeOff size={20} /> : <Eye size={20} />}
        </button>
      </div>
    </div>
  );

  const profileField = (label, value) => (
    <div className="flex min-w-0 items-start gap-3 rounded-lg border border-gray-100 bg-gray-50 p-4">
      <div className="min-w-0 flex-1">
        <p className="text-xs font-medium uppercase tracking-wide text-gray-500">
          {label}
        </p>
        <p className="mt-1 break-words text-sm font-semibold text-gray-800">
          {value || "Not available"}
        </p>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50 px-4 pb-12 pt-[88px] sm:px-6">
      <div className="mx-auto max-w-4xl">
        {/* Back navigation */}
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="mb-6 inline-flex items-center gap-2 text-sm font-medium text-gray-600 transition hover:text-indigo-700"
        >
          <ArrowLeft size={18} />
          Back
        </button>

        {/* Page heading */}
        <div className="mb-8">
          <div className="flex items-center gap-3">
            <div>
              <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl">
                My Account
              </h1>
            </div>
          </div>
        </div>

        {/* User information card */}
        <section className="mb-8 overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="border-b border-gray-200 px-6 py-5 sm:px-8">
            <h2 className="text-lg font-semibold text-gray-900">
              Personal Information
            </h2>
          </div>

          <div className="px-6 py-6 sm:px-8 sm:py-8">
            {loadingProfile ? (
              <div className="flex items-center justify-center gap-3 py-8 text-sm text-gray-500">
                <div className="h-5 w-5 animate-spin rounded-full border-2 border-indigo-600 border-t-transparent" />
                Loading your profile...
              </div>
            ) : (
              <>
                {profileError && (
                  <div
                    role="status"
                    className="mb-5 flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800"
                  >
                    <AlertCircle size={18} className="mt-0.5 shrink-0" />
                    <span>{profileError}</span>
                  </div>
                )}

                {/* Profile header */}
                <div className="mb-6 flex flex-col items-center gap-4 rounded-xl bg-indigo-50 px-4 py-6 sm:flex-row sm:px-6">
                  <div className="flex h-20 w-20 shrink-0 items-center justify-center rounded-full border-4 border-white bg-white text-indigo-700 shadow-sm">
                    <UserCircle size={64} strokeWidth={1.3} />
                  </div>

                  <div className="text-center sm:text-left">
                    <h3 className="text-xl font-bold text-gray-900">
                      {fullName}
                    </h3>
                    <p className="mt-1 break-all text-sm text-gray-600">
                      {email}
                    </p>
                    <span className="mt-3 inline-flex items-center gap-1.5 rounded-full bg-indigo-100 px-3 py-1 text-xs font-semibold text-indigo-800">
                      {role}
                    </span>
                  </div>
                </div>

                {/* Profile details */}
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  {profileField("First Name", userData?.firstName)}
                  {profileField("Last Name", userData?.lastName)}
                  {profileField("Email Address", email)}
                  {profileField("Account Role", role)}
                </div>
              </>
            )}
          </div>
        </section>

        {/* Password settings card */}
        <section className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm">
          <div className="border-b border-gray-200 px-6 py-5 sm:px-8">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-indigo-100 text-indigo-700">
                <LockKeyhole size={22} />
              </div>

              <div>
                <h2 className="text-lg font-semibold text-gray-900">
                  Change Password
                </h2>
              </div>
            </div>
          </div>

          <form
            onSubmit={handlePasswordChange}
            className="space-y-6 px-6 py-6 sm:px-8 sm:py-8"
          >
            {message && (
              <div
                role="alert"
                className={`flex items-start gap-3 rounded-lg border p-4 text-sm ${
                  messageType === "success"
                    ? "border-green-200 bg-green-50 text-green-800"
                    : "border-red-200 bg-red-50 text-red-700"
                }`}
              >
                {messageType === "success" ? (
                  <CheckCircle size={20} className="mt-0.5 shrink-0" />
                ) : (
                  <AlertCircle size={20} className="mt-0.5 shrink-0" />
                )}

                <span>{message}</span>
              </div>
            )}

            {passwordField(
              "Current Password",
              currentPassword,
              setCurrentPassword,
              showCurrentPassword,
              setShowCurrentPassword,
              "Enter your current password",
            )}

            <div className="space-y-6 border-t border-gray-100 pt-6">
              {passwordField(
                "New Password",
                newPassword,
                setNewPassword,
                showNewPassword,
                setShowNewPassword,
                "Enter your new password",
              )}

              <p className="-mt-4 text-xs text-gray-500">
                Your password must contain at least 6 characters.
              </p>

              {passwordField(
                "Confirm New Password",
                confirmPassword,
                setConfirmPassword,
                showConfirmPassword,
                setShowConfirmPassword,
                "Re-enter your new password",
              )}
            </div>

            <div className="flex flex-col-reverse gap-3 border-t border-gray-100 pt-6 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => {
                  setCurrentPassword("");
                  setNewPassword("");
                  setConfirmPassword("");
                  setMessage("");
                  setMessageType("");
                }}
                disabled={loading}
                className="rounded-lg border border-gray-300 px-5 py-3 text-sm font-semibold text-gray-700 transition hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Clear Fields
              </button>

              <button
                type="submit"
                disabled={loading}
                className="rounded-lg bg-indigo-700 px-6 py-3 text-sm font-semibold text-white transition hover:bg-indigo-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loading ? "Updating Password..." : "Update Password"}
              </button>
            </div>
          </form>
        </section>
      </div>
    </div>
  );
}
