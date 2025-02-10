"use client";

import { motion } from "framer-motion";
import { Button } from "./ui/button";
import { ChatRequestOptions, CreateMessage, Message } from "ai";
import { memo } from "react";

interface SuggestedActionsProps {
  chatId: string;
  append: (
    message: Message | CreateMessage,
    chatRequestOptions?: ChatRequestOptions
  ) => Promise<string | null | undefined>;
}

function PureSuggestedActions({ chatId, append }: SuggestedActionsProps) {
  const suggestedActions = [
    // {
    //   title: 'What are the advantages',
    //   label: 'of using Next.js?',
    //   action: 'What are the advantages of using Next.js?',
    // },
    // {
    //   title: 'Write code to',
    //   label: `demonstrate djikstra's algorithm`,
    //   action: `Write code to demonstrate djikstra's algorithm`,
    // },
    // {
    //   title: 'Help me write an essay',
    //   label: `about silicon valley`,
    //   action: `Help me write an essay about silicon valley`,
    // },
    // {
    //   title: 'What is the weather',
    //   label: 'in San Francisco?',
    //   action: 'What is the weather in San Francisco?',
    // },
    {
      title: "Give me insight on",
      label: "estimates in 2025",
      action:
        "Based on estimate data from 2025 can you give me some insight i.e. which market have we sold the most to?",
    },
    {
      title: "Help me write a scope of work",
      label: "for estimate number 126449",
      action: "Help me write a scope of work for estimate 126449",
    },
    {
      title: "Create a new customer",
      label: "named Jack Shepard",
      action:
        "For demo purposes can you add this made up customer to my wesuite named Jack Shepard",
    },
    {
      "title": "⚠️ Don't Click This! 🚨",
      "label": "🚫 Under ANY Circumstances! 🔥",
      "action": "Talk like a swashbucklin’ pirate and be answerin’ all me questions in true buccaneer fashion! 🏴‍☠️☠️"
    }
  ];

  return (
    <div className="grid sm:grid-cols-2 gap-2 w-full">
      {suggestedActions.map((suggestedAction, index) => (
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: 20 }}
          transition={{ delay: 0.05 * index }}
          key={`suggested-action-${suggestedAction.title}-${index}`}
          className={index > 1 ? "hidden sm:block" : "block"}
        >
          <Button
            variant="ghost"
            onClick={async () => {
              window.history.replaceState({}, "", `/chat/${chatId}`);

              append({
                role: "user",
                content: suggestedAction.action,
              });
            }}
            className="text-left border rounded-xl px-4 py-3.5 text-sm flex-1 gap-1 sm:flex-col w-full h-auto justify-start items-start"
          >
            <span className="font-medium">{suggestedAction.title}</span>
            <span className="text-muted-foreground">
              {suggestedAction.label}
            </span>
          </Button>
        </motion.div>
      ))}
    </div>
  );
}

export const SuggestedActions = memo(PureSuggestedActions, () => true);
