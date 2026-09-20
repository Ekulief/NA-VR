import { useNavigate } from "react-router-dom";
import { auth, db } from "../config/firebase-config";

import { signInWithEmailAndPassword } from "firebase/auth";

import { doc, getDoc } from "firebase/firestore";

import { useState } from "react";
import { Mail, Lock, Eye, EyeOff, Loader2 } from "lucide-react";
import bgImage from "../assets/polygon_bg.jpg";
import logo from "../assets/NA-VR_Logo.png";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const navigate = useNavigate();

  const signIn = async (e) => {
    e.preventDefault();

    setError("");
    setLoading(true);

    try {
      const userCredential = await signInWithEmailAndPassword(
        auth,
        email,
        password,
      );

      const user = userCredential.user;

      const userRef = doc(db, "user", user.uid);

      const userSnapshot = await getDoc(userRef);

      if (!userSnapshot.exists()) {
        await auth.signOut();
        setError("Your account is not registered in the system.");
        return;
      }

      const userData = userSnapshot.data();

      console.log("Logged in user:", userData);

      if (userData.role === "student") {
        navigate("/student");
      } else if (userData.role === "instructor") {
        navigate("/instructor");
      } else if (userData.role === "admin") {
        navigate("/admin");
      } else {
        await auth.signOut();
        setError("Your account does not have a valid role.");
      }
    } catch (error) {
      console.error(error);

      switch (error.code) {
        case "auth/invalid-credential":
          setError("Invalid email or password.");
          break;

        case "auth/user-not-found":
          setError("No account found with this email.");
          break;

        case "auth/wrong-password":
          setError("Incorrect password.");
          break;

        case "auth/invalid-email":
          setError("Please enter a valid email address.");
          break;

        default:
          setError("Something went wrong. Please try again.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      className="font-google min-h-screen flex items-center justify-center px-4 py-12 bg-cover bg-center bg-no-repeat"
      style={{ backgroundImage: `url(${bgImage})` }}
    >
      <div className="w-full max-w-md">
        <div className="bg-white/70 backdrop-blur-2xl border border-white shadow-2xl shadow-blue-900/10 rounded-3xl p-8 sm:p-10">
          <div className="flex justify-center mb-6">
            <img src={logo} className="w-25" />
          </div>

          <div className="text-center mb-8">
            <p className="text-slate-500 mt-2 text-md">Welcome Back!</p>
          </div>

          {error && (
            <div
              role="alert"
              className="flex items-start gap-2 bg-red-50 border border-red-200 text-red-600 text-sm px-4 py-3 rounded-xl mb-6 animate-[fadeIn_0.2s_ease-out]"
            >
              <span className="mt-0.5">⚠️</span>
              <span>{error}</span>
            </div>
          )}

          <form className="space-y-5" onSubmit={signIn} noValidate>
            <div>
              <label
                htmlFor="email"
                className="block text-sm font-medium text-slate-700 mb-1.5"
              >
                Email
              </label>
              <div className="relative">
                <Mail className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4.5 w-4.5 text-slate-400 pointer-events-none" />
                <input
                  id="email"
                  type="email"
                  autoComplete="email"
                  placeholder="you@example.com"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-10 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 placeholder-slate-400 transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-blue-500/40 focus:border-blue-500 focus:bg-white"
                />
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label
                  htmlFor="password"
                  className="block text-sm font-medium text-slate-700"
                >
                  Password
                </label>
                <a
                  href="#"
                  className="text-sm font-medium text-blue-600 hover:text-blue-700 transition-colors"
                >
                  Forgot password?
                </a>
              </div>
              <div className="relative">
                <Lock className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4.5 w-4.5 text-slate-400 pointer-events-none" />
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  placeholder="••••••••"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full pl-10 pr-11 py-3 bg-slate-50 border border-slate-200 rounded-xl text-slate-900 placeholder-slate-400 transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-blue-500/40 focus:border-blue-500 focus:bg-white"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 transition-colors"
                >
                  {showPassword ? (
                    <EyeOff className="h-4.5 w-4.5" />
                  ) : (
                    <Eye className="h-4.5 w-4.5" />
                  )}
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full flex items-center justify-center gap-2 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 disabled:from-slate-300 disabled:to-slate-300 disabled:cursor-not-allowed text-white font-semibold rounded-xl shadow-lg shadow-indigo-500/25 transition-all duration-200 active:scale-[0.98]"
            >
              {loading ? (
                <>
                  <Loader2 className="h-4.5 w-4.5 animate-spin" />
                  Logging in...
                </>
              ) : (
                "Login"
              )}
            </button>
          </form>
        </div>

        <p className="text-center text-sm text-slate-400 mt-6">
          Having trouble signing in? Contact your administrator.
        </p>
      </div>
    </div>
  );
}
