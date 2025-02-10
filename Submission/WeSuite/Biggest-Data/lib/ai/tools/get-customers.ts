import { tool } from "ai";
import { z } from "zod";

export const getCustomers = tool({
  description: "Get a list of customers from Wesuite",
  parameters: z.object({
      customerCount: z.number(),
      searchText: z.string()
  }),
  execute: async ({ customerCount, searchText }) => {
    const token = process.env.WESUITE_API_TOKEN;
    const baseUrl = process.env.WESUITE_API_URL;
    const response = await fetch(
      `${baseUrl}/v2/customers?searchModel.pageNumber=1&searchModel.pageSize=${customerCount}&searchModel.searchText=${searchText}`,
      {
        method: "GET",
        headers: {
          "Authorization": `Bearer ${token}`,
          "Content-Type": "application/json",
        },
      }
    );

    const customerData = await response.json();
    return customerData;
  },
});