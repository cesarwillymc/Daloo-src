// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly FlightBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(FlightBookingRecognizer luisRecognizer, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "Hola somos la polleria Pontifice? estamos a tu servicio.";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync("",null, cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case FlightBooking.Intent.Saludos:
         
                    var saludo = "Hola Como estas, ¿ deseas algun Pedido?";
                    var saludoFractory = MessageFactory.Text(saludo, saludo, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(saludoFractory, cancellationToken);
        
                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                     break;
                case FlightBooking.Intent.Precio:
         
                    var precio = "El costo total de su pedido es de 50 soles, ¿ Cual es su metodo de pago?";
                    var precioFractory = MessageFactory.Text(precio, precio, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(precioFractory, cancellationToken);
        
                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                     break;
                case FlightBooking.Intent.TipoPago :
         
                    var tipoPago = "Tu pedido ya esta saliendo estaremos en comunicacion con usted, Muchas Gracias por preferirnos.";
                    var tipoPagoFractory = MessageFactory.Text(tipoPago, tipoPago, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(tipoPagoFractory, cancellationToken);
        
                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                     break;
                case FlightBooking.Intent.Solicitud:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getWeatherMessageText = "Si anotaremos tu pedido cual es tu direccion!?";
                    var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;
                case FlightBooking.Intent.EnviarDireccion:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var message = "Esta bien estamos procesando tu pedido lo mas rapido que podemos, Desea algo mas? ";
                    var messagedfractory = MessageFactory.Text(message, message, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(messagedfractory, cancellationToken);
                    break;
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Disculpanos no podemos entender lo que intentas decir {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
       

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is BookingDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "esperando respuesta.";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}

