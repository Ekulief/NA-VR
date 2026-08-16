import { useEffect, useState } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { onAuthStateChanged } from "firebase/auth";
import { auth } from "./config/firebase-config";

import Navbar from "./components/Navbar";
import Login from "./pages/Login";
import StudentHome from "./pages/student/StudentHome";
import StudentCourseExperiments from "./pages/student/StudentCourseExperiments";
import StudentExperimentDetails from "./pages/student/StudentExperimentDetails";
import StudentExperimentBuilder from "./pages/student/StudentExperimentBuilder";
import InstructorHome from "./pages/instructor/InstructorHome";
import InstructorCourseExperiments from "./pages/instructor/InstructorCourseExperiments";
import InstructorExperimentBuilder from "./pages/instructor/InstructorExperimentBuilder";

function App() {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(auth, (currentUser) => {
      setUser(currentUser);
      setLoading(false);
    });

    return () => unsubscribe();
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-950 flex items-center justify-center">
        <p className="text-white text-lg">Loading...</p>
      </div>
    );
  }

  return (
    <BrowserRouter>
      {user && <Navbar />}

      <Routes>
        <Route
          path="/login"
          element={user ? <Navigate to="/" replace /> : <Login />}
        />
        <Route
          path="/student"
          element={user ? <StudentHome /> : <Navigate to="/login" replace />}
        />

        <Route
          path="/instructor"
          element={user ? <InstructorHome /> : <Navigate to="/login" replace />}
        />

        <Route
          path="/student/course/:blockId"
          element={
            user ? (
              <StudentCourseExperiments />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/student/course/:blockId/experiment/:experimentId"
          element={
            user ? (
              <StudentExperimentDetails />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/student/course/:blockId/experiment"
          element={
            user ? (
              <StudentExperimentDetails />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/student/course/:blockId/experiment/create"
          element={
            user ? (
              <StudentExperimentBuilder />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/student/course/:blockId/experiment/:experimentId/edit"
          element={
            user ? (
              <StudentExperimentBuilder />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/instructor/course/:blockId"
          element={
            user ? (
              <InstructorCourseExperiments />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/instructor/course/:blockId/experiment/create"
          element={
            user ? (
              <InstructorExperimentBuilder />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="/instructor/course/:blockId/experiment/:experimentId/edit"
          element={
            user ? (
              <InstructorExperimentBuilder />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        />

        <Route
          path="*"
          element={<Navigate to={user ? "/" : "/login"} replace />}
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
