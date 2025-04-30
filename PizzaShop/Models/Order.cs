
using System.ComponentModel.DataAnnotations;

namespace PizzaShop.Models
{
    public class Order
    {
        [Required(ErrorMessage = "The size field is required.")]
        public string? PizzaSize { get; set; }

        [Required(ErrorMessage = "The type field is required.")]
        public string? PizzaType { get; set; }

        public List<string>? Toppings { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [Display(Name = "Your email")] // Label text for the input field
        public string? Email { get; set; }

     
        [Display(Name = "Email me my order")] // Label text for the checkbox
        public bool SendEmailConfirmation { get; set; }
        public decimal TotalCost { get; set; }
        public string? OrderSummary { get; set; }

        public Order()
        {
            Toppings = new List<string>();
        }
    }
}