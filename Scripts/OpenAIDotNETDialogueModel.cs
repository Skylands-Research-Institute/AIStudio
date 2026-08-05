#if USE_COM_OPENAI_API
using OpenAI.Chat;
#endif
using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DracarysInteractive.AIStudio
{
    public class OpenAIDotNETDialogueModel : MonoBehaviour, IDialogueModel
    {
#if USE_COM_OPENAI_API
        public enum ReasoningEffort
        {
            ModelDefault,
            None,
            Low,
            Medium,
            High,
            XHigh,
            Max
        }

        public string apikey;
        public string model = "gpt-5.6-luna";
        public ReasoningEffort reasoningEffort = ReasoningEffort.ModelDefault;
        public int maxRetainedCompletions = 12; // Set to 0 or less to retain all completions.
        public bool async = false;
        public string[] messages; // Inspector debugging...

        private ChatCompletionOptions _options = new ChatCompletionOptions();
        private ChatClient _client;
        private List<ChatMessage> _messages = new List<ChatMessage>();
        private Queue<ChatMessage> _completions = new Queue<ChatMessage>();
        private List<string> _raw = new List<string>();

        void Awake()
        {
            DialogueModel.Instance.Log($"creating OpenAI .NET chat client, using model {model}");
            _client = new(model, string.IsNullOrEmpty(apikey) ? Environment.GetEnvironmentVariable("OPENAI_API_KEY") : apikey);

            if (reasoningEffort != ReasoningEffort.ModelDefault)
            {
                string effort = reasoningEffort == ReasoningEffort.XHigh
                    ? "xhigh"
                    : reasoningEffort.ToString().ToLowerInvariant();

#pragma warning disable OPENAI001
                _options.ReasoningEffortLevel = new ChatReasoningEffortLevel(effort);
#pragma warning restore OPENAI001
            }
        }

        public void SetTemperature(float temperature)
        {
            _options.Temperature = temperature;
        }

        public void SetMaxTokens(int maxTokens)
        {
            _options.MaxOutputTokenCount = maxTokens;
        }
        public string GetMessageJSON()
        {
            using MemoryStream stream = new();
            using Utf8JsonWriter writer = new(stream);

            writer.WriteStartArray();

            foreach (IJsonModel<ChatMessage> message in _messages)
            {
                message.Write(writer, ModelReaderWriterOptions.Json);
            }

            writer.WriteEndArray();
            writer.Flush();

            return BinaryData.FromBytes(stream.ToArray()).ToString();
        }

        private void Add(ChatMessage message)
        {
            _messages.Add(message);
            _raw.Add(message.Content[0].Text);
            messages = _raw.ToArray();
        }

        private void AddCompletion(string text)
        {
            ChatMessage completion = ChatMessage.CreateAssistantMessage(text);
            Add(completion);
            _completions.Enqueue(completion);

            if (maxRetainedCompletions <= 0)
                return;

            while (_completions.Count > maxRetainedCompletions)
            {
                ChatMessage staleCompletion = _completions.Dequeue();
                int index = _messages.IndexOf(staleCompletion);

                if (index >= 0)
                {
                    _messages.RemoveAt(index);
                    _raw.RemoveAt(index);
                }
            }

            messages = _raw.ToArray();
        }

        public void Clear()
        {
            DialogueModel.Instance.Log("enter Clear");
            _messages.Clear();
            _completions.Clear();
            _raw.Clear();
            messages = new string[0];
        }

        public void Complete(Action<string> onResponse, Action<UnityWebRequest> onError)
        {
            if (async)
                CompleteAsync(onResponse, onError);
            else
                CompleteSync(onResponse, onError);
        }

        private async void CompleteAsync(Action<string> onResponse, Action<UnityWebRequest> onError)
        {
            DialogueModel.Instance.Log("enter CompleteAsync");

            ChatCompletion completion = await _client.CompleteChatAsync(_messages, _options);

            string text = completion.Content[0].Text;

            DialogueModel.Instance.Log($"CompleteAsync: Add text={text}");

            AddCompletion(text);
            onResponse.Invoke(text);
        }

        private void CompleteSync(Action<string> onResponse, Action<UnityWebRequest> onError)
        {
            DialogueModel.Instance.Log("enter CompleteSync");

            ChatCompletion completion = _client.CompleteChat(_messages, _options);
            string text = completion.Content[0].Text;

            DialogueModel.Instance.Log($"CompleteAsync: Add text={text}");

            AddCompletion(text);
            onResponse.Invoke(text);
        }

        public void Prompt(string prompt)
        {
            DialogueModel.Instance.Log("enter Prompt");
            Add(ChatMessage.CreateSystemMessage(prompt));
        }

        public void Speak(string text)
        {
            DialogueModel.Instance.Log("enter Speak");
            Add(ChatMessage.CreateUserMessage(text));
        }

        public void Respond(string response)
        {
            DialogueModel.Instance.Log("enter Respond");
            Add(ChatMessage.CreateAssistantMessage(response));
        }
#else
        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Complete(Action<string> onResponse, Action<UnityWebRequest> onError)
        {
            throw new NotImplementedException();
        }

        public void Prompt(string prompt)
        {
            throw new NotImplementedException();
        }

        public void Respond(string text)
        {
            throw new NotImplementedException();
        }

        public void Speak(string text)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
