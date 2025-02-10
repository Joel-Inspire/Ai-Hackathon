import { motion } from "framer-motion";
import Link from "next/link";

import { MessageIcon, SparklesIcon, VercelIcon } from "./icons";

export const Overview = () => {
  return (
    <motion.div
      key="overview"
      className="max-w-3xl mx-auto md:mt-20"
      initial={{ opacity: 0, scale: 0.98 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.98 }}
      transition={{ delay: 0.5 }}
    >
      <div className="rounded-xl p-6 flex flex-col gap-8 leading-relaxed text-center max-w-xl">
        <p className="flex flex-row justify-center gap-4 items-center">
          <SparklesIcon size={24} />
          <span className="text-xl font-semibold">+</span>
          <MessageIcon size={24} />
        </p>
        <p>
          This is{" "}
          <span className="font-medium text-lg text-blue-500">JACK</span> (Just
          Another Chatbot for Knowledge), an AI-powered assistant designed to
          help salespeople interact with their WeSuite data.
        </p> 
      </div>
    </motion.div>
  );
};
