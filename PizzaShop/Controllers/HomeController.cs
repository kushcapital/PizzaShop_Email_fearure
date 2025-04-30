
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using PizzaShop.Models;
using System.Net;
using System.Net.Mail;


namespace PizzaShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        [HttpGet]
        public IActionResult Index()
        {

            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(Order order, string myButton)
        {

            if (myButton == "Clear")
            {
                _logger.LogInformation("Clearing order form.");
                ModelState.Clear(); // Clear validation state
                return View(new Order()); // Return a fresh view
            }

            // Handle "PreCompute Order" or "Place Order" button clicks [cite: 4]
            if (myButton == "PreCompute" || myButton == "PlaceOrder")
            {
                // Remove calculated fields from validation check as they are outputs
                ModelState.Remove("TotalCost");
                ModelState.Remove("OrderSummary");
             

                // Check if PizzaSize and PizzaType are provided [cite: 7, 8]
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("Model state is valid. Calculating order details.");
                    decimal totalCost = 0;

                    // Calculate base cost from Pizza Size
                    switch (order.PizzaSize)
                    {
                        case "Small": totalCost += 4.99m; break;
                        case "Medium": totalCost += 5.99m; break;
                        case "Large": totalCost += 6.99m; break;
                        case "X-Large": totalCost += 7.99m; break;
                        default:
                            _logger.LogWarning($"Invalid PizzaSize encountered: {order.PizzaSize}");
                            break;
                    }

                    // Add cost for each selected topping
                    if (order.Toppings != null && order.Toppings.Any())
                    {
                        totalCost += order.Toppings.Count * 0.99m;
                        _logger.LogInformation($"Added cost for {order.Toppings.Count} toppings.");
                    }
                    else
                    {
                        _logger.LogInformation("No toppings selected.");
                    }

                    order.TotalCost = totalCost;

                    // Build the order summary string - Base part
                    string toppingsList = (order.Toppings != null && order.Toppings.Count > 0)
                                            ? string.Join(", ", order.Toppings)
                                            : "None";

                    StringBuilder summaryBuilder = new StringBuilder();
                    summaryBuilder.AppendLine($"Toppings: {toppingsList}");
                    summaryBuilder.AppendLine($"Size: {order.PizzaSize}");
                    summaryBuilder.AppendLine($"Type: {order.PizzaType}");
                    summaryBuilder.Append($"Total amount: {totalCost:C}");

                    bool emailSentSuccessfully = false;

                    // *** ADD CONDITION FOR THANK YOU MESSAGE ***
                    if (myButton == "PlaceOrder")
                    {
                        _logger.LogInformation("Place Order button clicked.");
                        summaryBuilder.Append("; Thank you for your business!");

                        string emailBody = summaryBuilder.ToString();
                        string displaySummaryBase = emailBody.TrimEnd('\r', '\n');
                        summaryBuilder.Clear().Append(displaySummaryBase);


                        // --- Handle Email Sending ---
                        // Send email only if checkbox is checked (model property is true) and email address is valid/provided
                        if (order.SendEmailConfirmation && !string.IsNullOrWhiteSpace(order.Email))
                        {
                            _logger.LogInformation($"Email confirmation requested for {order.Email}.");
                            // Construct email subject
                            string emailSubject = $"Thank you for your business at: {DateTime.Now:h:mm:ss tt}"; // Format time

                            try
                            {
                                // Call the helper method to send the email using the prepared body
                                emailSentSuccessfully = SendOrderConfirmationEmail(
                                    recipientEmail: order.Email,
                                    subject: emailSubject,
                                    body: emailBody // Send the version with the "Thank you" line
                                );

                                if (emailSentSuccessfully)
                                {
                                    // Append "Email sent!" message to the summary shown on the page
                                    summaryBuilder.Append(" and Email sent!"); // Append to display summary
                                    _logger.LogInformation($"Confirmation email successfully sent to {order.Email}.");
                                }
                                else
                                {
                                    // Optionally add a message if sending failed
                                    summaryBuilder.Append(" (Email failed to send)");
                                   
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log unexpected exceptions during the email process
                                _logger.LogError(ex, $"Unexpected error during email sending process for {order.Email}");
                                summaryBuilder.Append(" (Error sending email)");
                                emailSentSuccessfully = false; // Ensure flag is false on exception
                            }
                        }
                        // If checkbox was checked but no email was provided (or invalid format stopped it earlier)
                        else if (order.SendEmailConfirmation && string.IsNullOrWhiteSpace(order.Email))
                        {
                            _logger.LogWarning("Email confirmation requested but no valid email address provided.");
                            summaryBuilder.Append(" (Please provide a valid email address to receive confirmation)");
                        }
                    }
                    // --- Handle "PreCompute" Specific Logic ---
                    else if (myButton == "PreCompute")
                    {
                        _logger.LogInformation("PreCompute Order button clicked.");
                        // For PreCompute, we only need the total, not "Thank You" or email messages.
                        // Rebuild the summary without those parts.
                        summaryBuilder.Clear(); 
                        summaryBuilder.AppendLine($"Toppings: {toppingsList}");
                        summaryBuilder.AppendLine($"Size: {order.PizzaSize}");
                        summaryBuilder.AppendLine($"Type: {order.PizzaType}");
                        summaryBuilder.Append($"Total amount: {totalCost:C}"); 
                    }

                    // Assign the final summary string to the model property for display in the textarea
                    order.OrderSummary = summaryBuilder.ToString();
                    _logger.LogInformation("Order processing complete. Returning view with order details.");
                    return View(order); 
                }
                else
                {
                 
                    _logger.LogWarning("Model state is invalid. Returning view with validation errors.");
                 
                    return View(order);
                }
            }

            // Fallback case if button value is unexpected
            _logger.LogWarning($"Unexpected button value encountered: {myButton}. Returning view.");
            return View(order);
        }


        /// <summary>
        /// Sends the order confirmation email using SMTP.
        /// </summary>
        /// <param name="recipientEmail">The customer's email address.</param>
        /// <param name="subject">The subject line for the email.</param>
        /// <param name="body">The plain text body of the email.</param>
        /// <returns>True if the email was sent successfully, false otherwise.</returns>
        private bool SendOrderConfirmationEmail(string recipientEmail, string subject, string body)
        {
      
            string senderEmail = "rajeev0458@gmail.com";     
            string senderPassword = "jymf ulyy uovx qiji ";     
            string senderDisplayName = "Cyber Pizza Shop";          

            // Basic check for placeholder credentials
            if (senderEmail.StartsWith("your_") || senderPassword.StartsWith("your_"))
            {
                _logger.LogError("Email sending failed: Sender email or password placeholders have not been replaced in HomeController.cs.");
               
                return false;
            }

            try
            {
                // Create the email message object
                MailMessage mm = new MailMessage();
                // Set sender address and display name (optional)
                mm.From = new MailAddress(senderEmail, senderDisplayName);
                // Add recipient from the order form
                mm.To.Add(new MailAddress(recipientEmail));
                // Set subject line according to requirements
                mm.Subject = subject;
                // Set email body content (plain text)
                mm.Body = body;
                // Specify body is plain text, not HTML
                mm.IsBodyHtml = false;

                // Configure the SMTP client for Gmail
                SmtpClient smtp = new SmtpClient("smtp.gmail.com"); // Gmail SMTP server address
                smtp.Port = 587;                                    // Gmail SMTP port for TLS
                smtp.EnableSsl = true;                              // Enable SSL/TLS encryption is required by Gmail
                smtp.UseDefaultCredentials = false;                 // We need to provide specific credentials

                // Create network credentials using sender email and password (App Password recommended)
                NetworkCredential credentials = new NetworkCredential(senderEmail, senderPassword);
                smtp.Credentials = credentials;                     // Assign credentials to the SMTP client

                _logger.LogInformation($"Attempting to send email via {smtp.Host}:{smtp.Port} from {senderEmail} to {recipientEmail}.");
                // Send the email
                smtp.Send(mm);
                _logger.LogInformation($"Successfully sent email to {recipientEmail}.");
                // Return true indicating success
                return true;
            }
            catch (SmtpException smtpEx)
            {
                
                _logger.LogError(smtpEx, $"SMTP Error sending email to {recipientEmail}. Status Code: {smtpEx.StatusCode}. Check credentials, Gmail 'App Password'/'Less Secure App' settings, and network connectivity.");
               
                return false; // Email sending failed
            }
            catch (Exception ex)
            {
                // Log any other unexpected errors during email sending
                _logger.LogError(ex, $"General Error sending email to {recipientEmail}.");
                return false; // Email sending failed
            }
        }


            [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public class ErrorViewModel
        {
            public string? RequestId { get; set; }
            public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        }
    }
}