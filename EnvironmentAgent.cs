/*
 * Author: Simon Powers
 * An Environment Agent that sends information to a Household Agent
 * about that household's demand, generation, and prices to buy and sell
 * from the utility company, on that day. Responds whenever pinged
 * by a Household Agent with a "start" message.
 * 
 * Amended by: Robbie Flockhart
 * Addition of an auction synchroniser that runs auctions one by one for 1 kWh
 * of energy at a time, one seller auctions 1 kWh of energy and then moves to
 * the back of the list to allow for other sellers in the neighbourhood to 
 * increase profits.
 */

using System;
using System.Collections.Generic;
using System.Text;
using ActressMas;
class EnvironmentAgent : Agent
{
    private Random rand = new Random();
    private List<string> sellers = new List<string>(); //list of all household agents classed as sellers
    private List<string> sellersEnergy = new List<string>(); //list of all sellers excess energy
    private List<string> buyers = new List<string>(); //list of all  household agents classed as buyers
    private List<string> bidders = new List<string>(); //list of all bidders for current auction
    private string highestBidder; //current highest bidder for current auction
    private int currentPrice; //current price for current auction
    private int reservePrice; //reserve price for curent auction
    private int turnsToWait = 100; // turn to wait before act default method is called.
    private bool auction = false; //is there an active auction
    private int householdsSatisfied = 0; //number of households with an equal energy demand and energy generation
    private List<string> households = new List<string>(); //list of all household agents
    private int noOfHouseholds; //number of household agents 
    private List<string> householdsType = new List<string>(); //list of household types (seller, buyer, N/A) for each individual household agent
    private List<string> householdsProfit = new List<string>(); //list of households profit for each individual household agent
    private List<string> householdsNoAuctionProfit = new List<string>(); //list of households profit if no auctions run for each individual household agent


    private const int MinGeneration = 5; //min possible generation from renewable energy on a day for a household (in kWh)
    private const int MaxGeneration = 15; //max possible generation from renewable energy on a day for a household (in kWh)
    private const int MinDemand = 5; //min possible demand on a day for a household (in kWh)
    private const int MaxDemand = 15; //max possible demand on a day for a household (in kWh)
    private const int MinPriceToBuyFromUtility = 12; //min possible price to buy 1kWh from the utility company (in pence)
    private const int MaxPriceToBuyFromUtility = 22; //max possible price to buy 1kWh from the utility company (in pence)
    private const int MinPriceToSellToUtility = 2; //min possible price to sell 1kWh to the utility company (in pence)
    private const int MaxPriceToSellToUtility = 5; //max possible price to sell 1kWh to the utility company (in pence)

    /* these variables are used in place of the originals to run one of the experiments for the report
     * 
    private const int MinPriceToBuyFromUtility = 6; //min possible price to buy 1kWh from the utility company (in pence)
    private const int MaxPriceToBuyFromUtility = 11; //max possible price to buy 1kWh from the utility company (in pence)
    */


    public override void Act(Message message)

    {
        Console.WriteLine($"\t{message.Format()}");
        message.Parse(out string action, out List<string> parameters);
        switch (action)
        {
            case "start": //this agent responds to "start" messages
                string senderID = message.Sender; //get the sender's name so we can reply to them
                int demand = rand.Next(MinDemand, MaxDemand); //the household's demand in kWh
                int generation = rand.Next(MinGeneration, MaxGeneration); //the household's demand in kWh
                int priceToBuyFromUtility = rand.Next(MinPriceToBuyFromUtility, MaxPriceToBuyFromUtility); //what the household's utility company
                                                                                                           //charges to buy 1kWh from it
                int priceToSellToUtility = rand.Next(MinPriceToSellToUtility, MaxPriceToSellToUtility);    //what the household's utility company
                                                                                                           //offers to buy 1kWh of renewable energy for
                string content = $"inform {demand} {generation} {priceToBuyFromUtility} {priceToSellToUtility}";
                Send(senderID, content); //send the message with this information back to the household agent that requested it
                break;
            case "seller": //this agent reacts to "seller" messages
                sellers.Add(message.Sender);
                sellersEnergy.Add(parameters[0]);
                noOfHouseholds += 1;
                break;
            case "buyer": //this agent reacts to "buyer" messages
                buyers.Add(message.Sender);
                noOfHouseholds += 1;
                break;
            case "householdNA": //this agent reacts to "householdNA" messages
                noOfHouseholds += 1;
                break;
            case "reservePrice": //this agent reacts to "reservePrice" messages
                reservePrice = Convert.ToInt32(parameters[0]);
                currentPrice = reservePrice;

                foreach (string b in buyers)
                {
                    Send(b, $"price {currentPrice}"); //sends a message containing the current price of the current auction to all household agents classed as buyers
                }
                break;
            case "bid": //this agent reacts to "bid" messages
                bidders.Add(message.Sender);
                break;
            case "householdSatisfied": //this agent reacts to "householdSatisfied" messages
                householdsSatisfied += 1;
                if (sellers.Contains(highestBidder))
                {
                    sellers.Remove(highestBidder);
                }
                if (sellers.Contains(message.Sender))
                {
                    sellers.Remove(message.Sender);
                }
                if (buyers.Contains(message.Sender))
                {
                    buyers.Remove(message.Sender);
                }
                if (householdsSatisfied == 5)
                {
                    Console.WriteLine("Finished");
                    Console.WriteLine("--- Households --- Household Type --- Household Profit --- No Auction Profit ---");
                    for (int i = 0; i < households.Count; i++)
                    {
                        Console.WriteLine(" " + households[i] + "      " + householdsType[i] + "                " + householdsProfit[i] + "                    " + householdsNoAuctionProfit[i]);
                    }
                    Stop();
                }
                break;
            case "data": //this agent reacts to "data" messages
                households.Add(message.Sender);
                householdsType.Add(parameters[0]);
                householdsProfit.Add(parameters[1]);
                householdsNoAuctionProfit.Add(parameters[2]);
                break;

            default:
                break;
        }
    }
    public override void ActDefault()
    {
        if (auction == false && noOfHouseholds == 5 && sellers.Count != 0)
        {
            Send(sellers[0], "auction"); //sends a message to the first seller in the sellers list to auction 1kWh of their energy
            auction = true;
            return;
            //skip 
        }
        if (auction == true)
        {
            if (--turnsToWait <= 0)
            {
                if (bidders.Count == 0) // no more bids
                {
                    currentPrice -= 1;
                    if (currentPrice < reservePrice)
                    {
                        //if there is no winner of the current auction
                        Console.WriteLine("[auctioneer]: Auction finished. No winner."); 
                        Broadcast("winner none"); //broadcasts a message to every agent to let them know that nobody won the current auction
                        Send(sellers[0], "notSold");//sends a message to the current auctions seller letting them know that their 1kWh of energy has not been sold
                        sellers.Remove(sellers[0]);
                        sellersEnergy.Remove(sellersEnergy[0]);
                    }
                    else
                    {
                        //else the buyer with the previous bid has won the current auction
                        Console.WriteLine($"[auctioneer]: Auction finished. Sold to {highestBidder} for price {currentPrice}.");
                        Broadcast($"winner {highestBidder} {currentPrice}"); //broadcasts a message to every agent to let them know that a buyer has won the current auction
                        Send(sellers[0], $"sold {currentPrice}"); //sends a message to the current auctions seller letting them know that their 1kWh of energy has been sold
                        string sellerEndOfList = sellers[0];
                        string sellerEnergyEndOfList = Convert.ToString(Convert.ToInt32(sellersEnergy[0]) - 1);
                        
                        sellers.Remove(sellers[0]);
                        sellersEnergy.Remove(sellersEnergy[0]);
                        if (Convert.ToInt32(sellerEnergyEndOfList) != 0)
                        {
                            sellers.Add(sellerEndOfList);     //moves seller to the back of the list
                            sellersEnergy.Add(sellerEnergyEndOfList);
                        }
                    }
                    //Stop();
                    auction = false;
                    turnsToWait = 100;

                    if (sellers.Count == 0)
                    {
                        foreach (string b in buyers)
                        {
                            Send(b, "noSellers"); //sends a message to all household agents classed as buyers that there are no more sellers left
                        }
                    }
                }
                else if (bidders.Count == 1)
                {
                    //if there is one bidder left then they have won the current auction
                    highestBidder = bidders[0];
                    Console.WriteLine($"[auctioneer]: Auction finished. Sold to {highestBidder} for price {currentPrice}");
                    Broadcast($"winner {highestBidder} {currentPrice}"); //broadcasts a message to every agent to let them know that a buyer has won the current auction
                    Send(sellers[0], $"sold {currentPrice}"); //sends a message to the current auctions seller letting them know that their 1kWh of energy has been sold
                    string sellerEndOfList = sellers[0];
                    string sellerEnergyEndOfList = Convert.ToString(Convert.ToInt32(sellersEnergy[0]) - 1);

                    if (Convert.ToInt32(sellerEnergyEndOfList) == 0)
                    {
                        buyers.Remove(sellers[0]);
                    }
                    sellers.Remove(sellers[0]);
                    sellersEnergy.Remove(sellersEnergy[0]);
                    if (Convert.ToInt32(sellerEnergyEndOfList) != 0)
                    {
                        sellers.Add(sellerEndOfList);      //moves seller to the back of the list
                        sellersEnergy.Add(sellerEnergyEndOfList);
                    }
                    //Stop();
                    auction = false;
                    
                    if (sellers.Count == 0)
                    {
                        foreach (string b in buyers)
                        {
                            Send(b, "noSellers"); //sends a message to all household agents classed as buyers that there are no more sellers left
                        }
                    }
                    bidders.Clear();
                    turnsToWait = 100;
                }
                else
                {
                    //if environment agent has received multiple bids 
                    highestBidder = bidders[0]; // first or random from the previous round, breaking ties
                    currentPrice += 1; //increments the asking price by 1 pence

                    foreach (string a in bidders)
                    {
                        Send(a, $"price {currentPrice}"); //sends a message containing the new asking price to all current bidders
                    }
                    bidders.Clear();
                    turnsToWait = 100;
                }
            }
        }
    }
}
