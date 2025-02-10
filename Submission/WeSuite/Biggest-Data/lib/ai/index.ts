import { openai } from '@ai-sdk/openai';
// import { experimental_wrapLanguageModel as wrapLanguageModel } from 'ai';

// import { customMiddleware } from './custom-middleware';

// export const customModel = (apiIdentifier: string) => {
//   return wrapLanguageModel({
//     model: openai(apiIdentifier),
//     middleware: customMiddleware,
//   });
// };

// export const imageGenerationModel = openai.image('dall-e-3');

import { azure } from "@ai-sdk/azure";
import { experimental_wrapLanguageModel as wrapLanguageModel } from "ai";

import { customMiddleware } from "./custom-middleware";

export const customModel = (apiIdentifier: string) => {
  return wrapLanguageModel({
    model: azure(apiIdentifier),
    middleware: customMiddleware,
  })
}

export const imageGenerationModel = openai.image('dall-e-3');
