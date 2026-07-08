using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using Fca.Mobile.Services;
using Microsoft.Maui.Controls;

namespace Fca.Mobile.Pages;

public partial class AuricruxPage : ContentPage
{
    private readonly FcaApiClient _apiClient;
    private readonly CustomerStore _store;
    
    private ObservableCollection<ChatMessage> _messages = new();
    private (string Message, string Reply)? _lastPair = null;
    
    private string _thinkingMode = "auto";
    private string _searchScope = "both";
    private bool _autoSpeak = true;
    
    private readonly List<string> _quickPrompts = new()
    {
        "How much materials do I need for this project?",
        "What are the safety hazards I should plan for?",
        "Walk me through the bid estimation process.",
        "What's the best sequence for this work?",
        "How do I estimate labor for a framing crew?",
        "Search industry standards for this material.",
        "Deep dive: construction project management challenges.",
    };

    public AuricruxPage(FcaApiClient apiClient, CustomerStore store)
    {
        InitializeComponent();
        
        _apiClient = apiClient;
        _store = store;
        
        // Initialize chat collection
        ChatCollection.ItemsSource = _messages;
        _messages.Add(new ChatMessage
        {
            Id = "m0",
            Role = "assistant",
            Text = "Auricrux Construction Expert is online. Ask me anything about construction: materials, safety, estimation, sequencing, labor, regulations, or best practices. I can think quickly, deeply, or automatically—and search internal knowledge or public sources."
        });
        
        // Set quick prompts
        PromptsCollection.ItemsSource = _quickPrompts;
        
        // Set initial button states
        UpdateThinkingModeButtons();
        UpdateSearchScopeButtons();
    }

    private void UpdateThinkingModeButtons()
    {
        QuickButton.BackgroundColor = _thinkingMode == "quick" ? Color.FromArgb("#1E3A8A") : Color.FromArgb("#FFFFFFA8");
        QuickButton.TextColor = _thinkingMode == "quick" ? Colors.White : Color.FromArgb("#2B3651");
        
        AutoButton.BackgroundColor = _thinkingMode == "auto" ? Color.FromArgb("#1E3A8A") : Color.FromArgb("#FFFFFFA8");
        AutoButton.TextColor = _thinkingMode == "auto" ? Colors.White : Color.FromArgb("#2B3651");
        
        DeepButton.BackgroundColor = _thinkingMode == "deep" ? Color.FromArgb("#1E3A8A") : Color.FromArgb("#FFFFFFA8");
        DeepButton.TextColor = _thinkingMode == "deep" ? Colors.White : Color.FromArgb("#2B3651");
    }

    private void UpdateSearchScopeButtons()
    {
        InternalButton.BackgroundColor = _searchScope == "internal" ? Color.FromArgb("#1E3A8A") : Color.FromArgb("#FFFFFFA8");
        InternalButton.TextColor = _searchScope == "internal" ? Colors.White : Color.FromArgb("#2B3651");
        
        PublicButton.BackgroundColor = _searchScope == "public" ? Color.FromArgb("#1E3A8A") : Color.FromArgb("#FFFFFFA8");
        PublicButton.TextColor = _searchScope == "public" ? Colors.White : Color.FromArgb("#2B3651");
        
        BothButton.BackgroundColor = _searchScope == "both" ? Color.FromArgb("#1E3A8A") : Color.FromArgb("#FFFFFFA8");
        BothButton.TextColor = _searchScope == "both" ? Colors.White : Color.FromArgb("#2B3651");
    }

    private void OnThinkingModeSelected(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            _thinkingMode = button.Text switch
            {
                "⚡ Quick" => "quick",
                "🤖 Auto" => "auto",
                "🧠 Deep" => "deep",
                _ => "auto"
            };
            UpdateThinkingModeButtons();
        }
    }

    private void OnSearchScopeSelected(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            _searchScope = button.Text switch
            {
                "📚 Internal" => "internal",
                "🌐 Public" => "public",
                "🔄 Both" => "both",
                _ => "both"
            };
            UpdateSearchScopeButtons();
        }
    }

    private void OnMessageTextChanged(object sender, TextChangedEventArgs e)
    {
        SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageEntry.Text);
    }

    private void OnQuickPromptClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.Text is string prompt)
        {
            MessageEntry.Text = prompt;
        }
    }

    private async void OnSendMessage(object sender, EventArgs e)
    {
        string message = MessageEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(message) || LoadingIndicator.IsRunning)
            return;

        // Add user message
        _messages.Add(new ChatMessage
        {
            Id = $"u-{DateTime.UtcNow.Ticks}",
            Role = "user",
            Text = message
        });

        MessageEntry.Text = "";
        SendButton.IsEnabled = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            // Create request payload
            var payload = new
            {
                message = message,
                route = "/construction",
                context = new
                {
                    source = "auricrux-desktop-maui",
                    specialistAgent = "construction-expert",
                    thinkingMode = _thinkingMode,
                    searchScope = _searchScope,
                    tone = "professional-practical"
                }
            };

            // Call backend
            using var httpClient = new HttpClient { BaseAddress = new Uri("https://auricrux-central.azurewebsites.net") };
            var response = await httpClient.PostAsJsonAsync("/api/auricrux", payload);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"API returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("reply", out var replyElement) && replyElement.ValueKind == JsonValueKind.String)
            {
                string reply = replyElement.GetString() ?? "No response from Auricrux.";
                
                _messages.Add(new ChatMessage
                {
                    Id = $"a-{DateTime.UtcNow.Ticks}",
                    Role = "assistant",
                    Text = reply
                });

                _lastPair = (message, reply);

                // Auto-speak if enabled
                if (_autoSpeak)
                {
                    await Speak(reply);
                }
            }
            else
            {
                throw new InvalidOperationException("Invalid response format from Auricrux.");
            }
        }
        catch (Exception ex)
        {
            string errorText = ex.Message;
            _messages.Add(new ChatMessage
            {
                Id = $"e-{DateTime.UtcNow.Ticks}",
                Role = "assistant",
                Text = $"Connection issue: {errorText}"
            });
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            SendButton.IsEnabled = true;
        }
    }

    private async void OnFeedbackUp(object sender, EventArgs e)
    {
        await SendFeedback("up");
    }

    private async void OnFeedbackDown(object sender, EventArgs e)
    {
        await SendFeedback("down");
    }

    private async Task SendFeedback(string rating)
    {
        if (_lastPair is null)
        {
            await DisplayAlert("No response yet", "Send a message first so feedback can be attached.", "OK");
            return;
        }

        try
        {
            var payload = new
            {
                rating = rating,
                message = _lastPair.Value.Message,
                reply = _lastPair.Value.Reply,
                route = "/construction",
                context = new
                {
                    source = "auricrux-desktop-maui",
                    specialistAgent = "construction-expert"
                }
            };

            using var httpClient = new HttpClient { BaseAddress = new Uri("https://auricrux-central.azurewebsites.net") };
            var response = await httpClient.PostAsJsonAsync("/api/auricrux", payload);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Feedback recorded", $"Saved: {rating}", "OK");
            }
            else
            {
                await DisplayAlert("Feedback failed", "Could not save feedback", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnVoiceInput(object sender, EventArgs e)
    {
        await DisplayAlert("Voice input", "Push-to-talk is coming soon. Type your message for now.", "OK");
    }

    private void OnToggleSpeaker(object sender, EventArgs e)
    {
        _autoSpeak = !_autoSpeak;
        SpeakerButton.Text = _autoSpeak ? "🔊 On" : "🔇 Off";
        SpeakerButton.BackgroundColor = _autoSpeak ? Color.FromArgb("#D1FDB8") : Color.FromArgb("#FFFFFFC0");
    }

    private async Task Speak(string text)
    {
        try
        {
            if (text.Length > 0)
            {
                await TextToSpeech.SpeakAsync(new SpeechOptions
                {
                    Locale = new Locale(new CultureInfo("en-US")),
                    Volume = (float)0.9,
                    Pitch = 1.0f
                }, cancellationToken: CancellationToken.None);

                // For actual speech, use the text parameter with TextToSpeech
                await TextToSpeech.SpeakAsync(text);
            }
        }
        catch
        {
            // TTS not available - silently continue
        }
    }
}

public class ChatMessage
{
    public string Id { get; set; }
    public string Role { get; set; } // "user" or "assistant"
    public string Text { get; set; }
}
