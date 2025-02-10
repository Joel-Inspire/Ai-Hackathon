import { generateUUID } from '@/lib/utils';
import {
  DataStreamWriter,
  smoothStream,
  streamText,
  tool,
} from 'ai';
import { z } from 'zod';
import { customModel } from '..';
import { saveDocument } from '@/lib/db/queries';
import { Session } from 'next-auth';
import { Model } from '../models';
import { FirecrawlClient } from '@agentic/firecrawl';

const firecrawl = new FirecrawlClient();
const knowledgeBaseUrl = 'https://support.wesuite.com/hc/en-us';

interface SupportAgentProps {
  model: Model;
  session: Session;
  dataStream: DataStreamWriter;
}

export const supportAgent = ({
  model,
  session,
  dataStream,
}: SupportAgentProps) =>
  tool({
    description:
      'Provide support by retrieving relevant information from the WeSuite knowledge base on products WeEstimate, WeOpportunity and QuoteAnywhere also release notes and generating a helpful response.',
    parameters: z.object({
      query: z.string(),
    }),
    execute: async ({ query }) => {
      const id = generateUUID();
      let responseText = '';

      dataStream.writeData({ type: 'id', content: id });
      dataStream.writeData({ type: 'query', content: query });
      dataStream.writeData({ type: 'clear', content: '' });

      // Crawl the knowledge base for relevant information
        const crawlResult = await firecrawl.scrapeUrl({ url: knowledgeBaseUrl });
      const knowledgeData = crawlResult.data?.content || 'No relevant support data found.';

      // Generate a response using LLM
      const { fullStream } = streamText({
        model: customModel(model.apiIdentifier),
        system:
          'You are a knowledgeable support agent for WeSuite. Based on the user query and retrieved knowledge base and release note data, provide a clear and professional response.',
        experimental_transform: smoothStream({ chunking: 'word' }),
        prompt: `User Query: ${query}\n\nKnowledge Base Data:\n${knowledgeData}`,
      });

      for await (const delta of fullStream) {
        if (delta.type === 'text-delta') {
          responseText += delta.textDelta;
          dataStream.writeData({ type: 'text-delta', content: delta.textDelta });
        }
      }

      // Finish response
      dataStream.writeData({ type: 'finish', content: '' });

      // Save interaction if the user is authenticated
      if (session.user?.id) {
        await saveDocument({
          id,
          title: `Support Query: ${query}`,
          kind: 'text',
          content: responseText,
          userId: session.user.id,
        });
      }

      return {
        id,
        title: `Support Response for: ${query}`,
        kind: 'text',
        content: responseText || 'No relevant support information found.',
      };
    },
  });