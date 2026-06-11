import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import "./index.css";
import App from "./App.tsx";
import FeedPage from "./pages/FeedPage.tsx";
import CreateQuestPage from "./pages/CreateQuestPage.tsx";
import QuestDetailPage from "./pages/QuestDetailPage.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />}>
          <Route index element={<FeedPage />} />
          <Route path="quests/new" element={<CreateQuestPage />} />
          <Route path="quests/:id" element={<QuestDetailPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>
);
