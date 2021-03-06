﻿using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LunchBox.Dialogs
{
    [Serializable]
    public class HungryDialog : IDialog<Recommendation>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("You're hungry? Ok, I can help you with that.");
            context.Wait(HungryMessageReceivedAsync);
        }

        public virtual async Task HungryMessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var activity = await result as Activity;

            Criteria criteria;
            if (!context.PrivateConversationData.TryGetValue("criteria", out criteria))
            {
                criteria = new Criteria();
                context.PrivateConversationData.SetValue("criteria", criteria);
            }

            await AskNextQuestion(criteria, context, activity.Text);
        }

        private static bool CheckForCancel(IDialogContext context, string message)
        {
            if (Responses.CancelResponses.Contains(message))
            {
                // The user wants to cancel, so let's complete this dialog
                context.Done((Recommendation)null);
                return true;
            }

            return false;
        }

        private Recommendation DetermineTheBestPlaceToGoToLunch(Criteria criteria)
        {
            // TODO: determine where you should go based on the criteria
            return new Recommendation();
        }

        // Asks a question and lets the calling code know if this code ended up asking a question by returning true.
        private async Task AskNextQuestion(Criteria criteria, IDialogContext context, string currentMessage)
        {
            if (criteria.Attendees == null || !criteria.Attendees.Any())
            {
                await context.PostAsync("Who is going to lunch with you?");
                context.Wait(AttendeesResponseReceived);
            }
            else if (!criteria.HasTimeRestrictions.HasValue)
            {
                var nextMessage = context.MakeMessage();
                nextMessage.Text = "Do you have any time restrictions?";
                nextMessage.Type = ActivityTypes.Message;
                nextMessage.SuggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction>
                    {
                        new CardAction() {
                            Type = ActionTypes.ImBack,
                            Title = "Yes",
                            Value = "Yes"
                        },
                        new CardAction() {
                            Type = ActionTypes.ImBack,
                            Title = "No",
                            Value = "No"
                        }
                    }
                };
                await context.PostAsync(nextMessage);
                context.Wait(TimeRestrictionsResponseReceived);
            }
            else if (criteria.HasTimeRestrictions.GetValueOrDefault() && !criteria.LunchDuration.HasValue)
            {
                await context.PostAsync("How long of a lunch do you have time for?");
                context.Wait(LunchDurationResponseReceived);
            }
            else
            {
                var recommendation = DetermineTheBestPlaceToGoToLunch(criteria);
                // Create a hero card for the pick and return back the recommendation.
                context.Done(recommendation);
            }
        }

        private async Task LunchDurationResponseReceived(IDialogContext context, IAwaitable<object> result)
        {
            var response = await result as Activity;
            if (CheckForCancel(context, response.Text))
                return;

            var criteria = context.PrivateConversationData.GetValue<Criteria>("criteria");

            // For now, just accept the number of minutes
            var durationText = response.Text.Replace("minutes", "").Trim();
            short duration;
            if (!short.TryParse(durationText, out duration))
            {
                await context.PostAsync("Sorry, I didn't catch that. I'm a bit new here. Can you tell me how many minutes you have to eat lunch?");
                context.Wait(LunchDurationResponseReceived);
            }

            criteria.LunchDuration = new TimeSpan(0, duration, 0);
            context.PrivateConversationData.SetValue("criteria", criteria);

            await AskNextQuestion(criteria, context, response.Text);
        }

        private async Task TimeRestrictionsResponseReceived(IDialogContext context, IAwaitable<object> result)
        {
            var response = await result as Activity;
            if (CheckForCancel(context, response.Text))
                return;

            var criteria = context.PrivateConversationData.GetValue<Criteria>("criteria");

            criteria.HasTimeRestrictions = !Responses.NegativeResponses.Contains(response.Text.ToLower());
            context.PrivateConversationData.SetValue("criteria", criteria);

            await AskNextQuestion(criteria, context, response.Text);
        }

        private async Task AttendeesResponseReceived(IDialogContext context, IAwaitable<object> result)
        {
            var response = await result as Activity;
            if (CheckForCancel(context, response.Text))
                return;

            var criteria = context.PrivateConversationData.GetValue<Criteria>("criteria");

            criteria.Attendees = response.Text
               .Replace("and", "")
               .Split(new[] { ',', ' ' })
               .Where(p => !string.IsNullOrEmpty(p))
               .Select(p => p.Trim())
               .ToList();

            context.PrivateConversationData.SetValue("criteria", criteria);
            await AskNextQuestion(criteria, context, response.Text);
        }
    }
}