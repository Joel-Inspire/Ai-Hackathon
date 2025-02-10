

import { tool, streamObject } from "ai";
import { z } from "zod";
import { DataStreamWriter } from "ai";
import { Model } from "../models";
import { customModel } from "..";
import { Session } from 'next-auth';
import { saveDocument } from "@/lib/db/queries";
import { generateUUID } from "@/lib/utils";

interface GetEstimatesProps {
  model: Model;
  session: Session;
  dataStream: DataStreamWriter;
}

export const getEstimates = ({
model,
  session,
  dataStream,
}: GetEstimatesProps) =>
  tool({
    description: "Get estimate/job sales data from WeSuite and analyze insights",
    parameters: z.object({
      year: z.number().describe("The year to fetch sales estimates for"),
    }),
      execute: async ({ year }) => {
        

          const token = process.env.WESUITE_API_TOKEN;
          const baseUrl = process.env.WESUITE_API_URL;
      const response = await fetch(
        `${baseUrl}/hackathon/estimateQuery?year=${year}`,
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

      const { elementStream } = streamObject({
        model: customModel(model.apiIdentifier),
        system:
          "You are a sales data analyst. Given a set of sales estimates, analyze trends, top-performing categories, and key insights. Provide an overview of the data, highlight any patterns, and suggest business actions.",
        prompt: JSON.stringify(estimateData, null, 2),
        output: "array",
        schema: z.object({
          keyInsight: z.string().describe("A key insight from the data"),
          trendAnalysis: z.string().describe("Analysis of sales trends"),
          recommendedAction: z
            .string()
            .describe("Suggested actions based on the insights"),
        }),
      });

      const insights = [];

      for await (const element of elementStream) {
        const insight = {
          keyInsight: element.keyInsight,
          trendAnalysis: element.trendAnalysis,
          recommendedAction: element.recommendedAction,
        };

        dataStream.writeData({
          type: "insight",
          content: insight,
        });
          
        

        insights.push(insight);
      }
          console.log(insights)

          const id = generateUUID();

          
          

          return {
              id,
              year,
              title: 'yo',
            kind: 'text',
              message: "Sales insights generated successfully",
              insights
          };
    },
  });
