import {
  type Message,
  createDataStreamResponse,
  smoothStream,
  streamText,
} from 'ai';

import { auth } from '@/app/(auth)/auth';
import { customModel } from '@/lib/ai';
import { models } from '@/lib/ai/models';
import { systemPrompt } from '@/lib/ai/prompts';
import {
  deleteChatById,
  getChatById,
  saveChat,
  saveMessages,
} from '@/lib/db/queries';
import {
  generateUUID,
  getMostRecentUserMessage,
  sanitizeResponseMessages,
} from '@/lib/utils';

import { generateTitleFromUserMessage } from '../../actions';
import { createDocument } from '@/lib/ai/tools/create-document';
import { updateDocument } from '@/lib/ai/tools/update-document';
import { requestSuggestions } from '@/lib/ai/tools/request-suggestions';
import { getWeather } from '@/lib/ai/tools/get-weather';
import { getCustomers } from '@/lib/ai/tools/get-customers';
import { getEstimates } from '@/lib/ai/tools/get-estimates';
import { createCustomers } from '@/lib/ai/tools/create-customer';
import { createEstimateDocument } from '@/lib/ai/tools/create-sow';
import { supportAgent } from '@/lib/ai/tools/crawl';

export const maxDuration = 60;

type AllowedTools =
  | 'createDocument'
  | 'updateDocument'
  | 'requestSuggestions'
  | 'getWeather'
  | 'getCustomers'
  | 'getEstimates'
  | 'createCustomers'
  | 'createEstimateDocument'
  | 'supportAgent';

const blocksTools: AllowedTools[] = [
  'createDocument',
  'updateDocument',
  'requestSuggestions',
];

const weatherTools: AllowedTools[] = ['getWeather'];
const customerTools: AllowedTools[] = ['getCustomers', 'createCustomers'];
const estimateTools: AllowedTools[] = ['getEstimates', 'createEstimateDocument'];
const supportAgentTools: AllowedTools[] = ['supportAgent'];
const allTools: AllowedTools[] = [...blocksTools, ...weatherTools, ...customerTools, ...estimateTools, ...supportAgentTools];

export async function POST(request: Request) {
  const {
    id,
    messages,
    modelId,
  }: { id: string; messages: Array<Message>; modelId: string } =
    await request.json();

  const session = await auth();

  if (!session || !session.user || !session.user.id) {
    return new Response('Unauthorized', { status: 401 });
  }

  const model = models.find((model) => model.id === modelId);

  if (!model) {
    return new Response('Model not found', { status: 404 });
  }

  const userMessage = getMostRecentUserMessage(messages);

  if (!userMessage) {
    return new Response('No user message found', { status: 400 });
  }

  const chat = await getChatById({ id });

  if (!chat) {
    const title = await generateTitleFromUserMessage({ message: userMessage });
    await saveChat({ id, userId: session.user.id, title });
  }

  await saveMessages({
    messages: [{ ...userMessage, createdAt: new Date(), chatId: id }],
  });

  return createDataStreamResponse({
    execute: async (dataStream) => {
      try {
        console.log("model:", model.apiIdentifier);
    
        const result = streamText({
          model: customModel(model.apiIdentifier),
          system: systemPrompt,
          messages,
          maxSteps: 5,
          experimental_activeTools: allTools,
          experimental_transform: smoothStream({ chunking: 'word' }),
          experimental_generateMessageId: generateUUID,
          tools: {
            getWeather,
            getCustomers,
            createCustomers,
            supportAgent: supportAgent({session, dataStream, model}),
            createEstimateDocument: createEstimateDocument({session, dataStream, model}),
            getEstimates: getEstimates({session, dataStream, model}),
            createDocument: createDocument({ session, dataStream, model }),
            updateDocument: updateDocument({ session, dataStream, model }),
            requestSuggestions: requestSuggestions({
              session,
              dataStream,
              model,
            }),
          },
          onFinish: async ({ response }) => {
            try {
              if (session.user?.id) {
                const responseMessagesWithoutIncompleteToolCalls =
                  sanitizeResponseMessages(response.messages);
    
                await saveMessages({
                  messages: responseMessagesWithoutIncompleteToolCalls.map(
                    (message) => ({
                      id: message.id,
                      chatId: id,
                      role: message.role,
                      content: message.content,
                      createdAt: new Date(),
                    })
                  ),
                });
              }
            } catch (error) {
              console.error("Failed to save chat:", error);
            }
          },
          experimental_telemetry: {
            isEnabled: true,
            functionId: 'stream-text',
          },
        });
    
        result.mergeIntoDataStream(dataStream);
      } catch (error) {
        console.error("Error in execute function:", error);
      }
    } 
  });
}

export async function DELETE(request: Request) {
  const { searchParams } = new URL(request.url);
  const id = searchParams.get('id');

  if (!id) {
    return new Response('Not Found', { status: 404 });
  }

  const session = await auth();

  if (!session || !session.user) {
    return new Response('Unauthorized', { status: 401 });
  }

  try {
    const chat = await getChatById({ id });

    if (chat.userId !== session.user.id) {
      return new Response('Unauthorized', { status: 401 });
    }

    await deleteChatById({ id });

    return new Response('Chat deleted', { status: 200 });
  } catch (error) {
    return new Response('An error occurred while processing your request', {
      status: 500,
    });
  }
}
