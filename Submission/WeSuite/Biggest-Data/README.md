<h1 align="center">WeSuite AI Chatbot</h1>


<p align="center">
  An AI Chatbot Built With Next.js and the AI SDK by Vercel to interacte with WeSuite data in real time.
</p>

User data and chat history is stored in cloud hosted postgres db.

Using Vercel AI SDK Tools to connect to WeSuite API, the model will be decide at runtime which tool is the best fit for the users prompt.

Using multi-step calls during the conversation with the bot if the LLM does not have the data it needs it will look up the data with a tool call.  The result in this case will be data from the WeSuite API and passed back to the LLM to answers the users prompt.

## Model Providers

Using gpt-4o/gpt-4o-mini hosted in azure.

## Running locally

```bash
pnpm install
pnpm dev
```

App should now be running on [localhost:3000](http://localhost:3000/).
