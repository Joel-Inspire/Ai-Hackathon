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

interface CreateEstimateDocumentProps {
  model: Model;
  session: Session;
  dataStream: DataStreamWriter;
}

export const createEstimateDocument = ({
  model,
  session,
  dataStream,
}: CreateEstimateDocumentProps) =>
  tool({
    description:
      'Create a scope of work for an estimate by calling the /v2/estimates/{estimateId} endpoint and generating content based on the returned data.',
    parameters: z.object({
      estimateId: z.string(),
    }),
    execute: async ({ estimateId }) => {
      const id = generateUUID();
      let draftText = '';

      dataStream.writeData({
        type: 'id',
        content: id,
      });

      dataStream.writeData({
        type: 'estimateId',
        content: estimateId,
      });

      dataStream.writeData({
        type: 'clear',
        content: '',
      });

        const token = process.env.WESUITE_API_TOKEN;
        const baseUrl = process.env.WESUITE_API_URL;
      const response = await fetch(
        `${baseUrl}/v2/estimates/${estimateId}?IncludeAllJobItems=true&DisableOwnershipFiltering=true`,
        {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        }
      );

      const estimateData = await response.json();

        if (!estimateData || estimateData.length === 0) {
            return { error: "No estimates found for the given year." };
        }


        
        const { fullStream } = streamText({
          model: customModel(model.apiIdentifier),
          system:
            'You are a sales professional. Based on the provided estimate data, create a detailed scope of work that can be presented to a customer. The scope of work should be clear, professional, and highlight the key tasks and deliverables in a format that is easy to understand for the customer.',
          experimental_transform: smoothStream({ chunking: 'word' }),
          prompt: JSON.stringify(estimateData),
        });

                for await (const delta of fullStream) {
          const { type } = delta;

          if (type === 'text-delta') {
            const { textDelta } = delta;

            draftText += textDelta;
            dataStream.writeData({
              type: 'text-delta',
              content: textDelta,
            });
          }
        }



      dataStream.writeData({ type: 'finish', content: '' });

      if (session.user?.id) {
        await saveDocument({
          id,
          title: `Estimate ${estimateId}`,
          kind: 'text',
          content: draftText,
          userId: session.user.id,
        });
      }

      return {
        id,
        title: `Estimate ${estimateId}`,
        kind: 'text',
        content: 'A document was created with the estimate details.',
      };
    },
  });
