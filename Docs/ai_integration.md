# AI Integration Guide

Conda supports AI-assisted coding features like autocompletion and code generation. To enable these features, you need to configure an API key.

## Security First

Conda uses a `.env` file to store sensitive information like API keys. This file is included in `.gitignore` by default to prevent accidental sharing.

## How to get free API keys

### 1. Google Gemini (Recommended)

1. Go to [Google AI Studio](https://aistudio.google.com/).
2. Sign in with your Google account.
3. Click on **Get API key**.
4. Create a new API key in a new project.
5. Copy the key.

### 2. Groq Cloud

1. Visit [Groq Cloud Console](https://console.groq.com/).
2. Sign up or log in.
3. Go to the **API Keys** section.
4. Create a new key and copy it.

### 3. Hugging Face

1. Go to [Hugging Face Settings](https://huggingface.co/settings/tokens).
2. Create a **New Token** with `read` access.
3. Copy the token.

## Setup Instructions

1. Locate the `.env` file in the root of your project.
2. If it doesn't exist, create one or copy `.env.example`.
3. Add your key like this:

   ```env
   AI_API_KEY=your_key_here
   ```

4. Restart Conda (if it's running) to apply the changes.

The AI Assistant section in the Settings view provides a quick button to open this file for you.
