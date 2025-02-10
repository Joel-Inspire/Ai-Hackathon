import { tool } from "ai";
import { z } from "zod";

export const createCustomers = tool({
  description: "Create a customer in wesuite",
  parameters: z.object({
      name: z.string(),
  }),
  execute: async ({ name }) => {
      const token = process.env.WESUITE_API_TOKEN;
      const baseUrl = process.env.WESUITE_API_URL;
      const data = { 
        Name:name,
    };

const jsonData = JSON.stringify(data);
    const response = await fetch(
      `${baseUrl}/v2/customers`,
      {
        method: "POST",
        headers: {
          "Authorization": `Bearer ${token}`,
          "Content-Type": "application/json",
          },
          body: jsonData
      }
    );

    const customerData = await response.json();
    return customerData;
  },
});