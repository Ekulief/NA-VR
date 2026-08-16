import { initializeApp } from "firebase/app";
import { getAuth } from "firebase/auth";
import { getFirestore } from "firebase/firestore";
import { getStorage } from "firebase/storage";

const firebaseConfig = {
  apiKey: "AIzaSyDx4woU60eYGU3iUIbRNi3emMIM5f2PKVc",
  authDomain: "slubt-labs-5e96f.firebaseapp.com",
  projectId: "slubt-labs-5e96f",
  storageBucket: "slubt-labs-5e96f.firebasestorage.app",
  messagingSenderId: "436564810069",
  appId: "1:436564810069:web:926ab78e97bcb91e4d37dc",
  measurementId: "G-791GEGCZB5"
};

const app = initializeApp(firebaseConfig);
export const auth = getAuth(app);
export const db = getFirestore(app);
export const storage = getStorage(app);