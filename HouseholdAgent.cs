using ActressMas;
using System;
using System.Collections.Generic;

namespace MASCWK
{
    class HouseholdAgent : Agent
    {
        private int demand; //energy demand in kWh
        private int generation; //energy generation in kWh
        private int priceToBuyFromUtility; //price to buy 1 kWh of energy from the utility company
        private int priceToSellToUtility; //price to sell 1 kWh of energy from the utility company
        private bool buyer; //is the household agent a classed as a buyer
        private bool seller; //is the household agent classed as a sell
        private int energy; //if seller - amount of energy that needs to be sold, if buyer - amount of energy that needs to be bought
        private int energyValue; //if seller - the minimum value that they will sell 1kWh of their energy for, if buyer - the maximum value that they will buy 1 kWh of energy for
        private int profit = 0; //positive if seller, negative if buyer
        private int noAuctionProfit = 0; //positive if seller, negative if buyer - when no auction are run

        public override void Setup()
        {
            Send("environmentAgent", "start"); //sends a message to the environment agent to start the model are get their demand, generation, priceToBuyFromUtility, and priceToSellToUtility
        }

        public override void Act(Message message)
        {
            try
            {
                Console.WriteLine($"\t{message.Format()}");
                message.Parse(out string action, out List<string> parameters);

                switch (action)
                {
                    case "inform": //this agent reacts to "inform" messages
                        demand = Convert.ToInt32(parameters[0]);
                        generation = Convert.ToInt32(parameters[1]);
                        priceToBuyFromUtility = Convert.ToInt32(parameters[2]);
                        priceToSellToUtility = Convert.ToInt32(parameters[3]);

                        if(demand > generation)
                        {
                            //if household agent is a buyer
                            buyer = true;
                            energy = demand - generation;
                            energyValue = priceToBuyFromUtility;
                            noAuctionProfit -= (priceToBuyFromUtility * energy);
                            Send("environmentAgent", "buyer"); //sends a message to environment agent to let it know that this household agent is a buyer
                        }
                        else if (demand < generation)
                        {
                            //if household agent is a seller
                            seller = true;
                            energy = generation - demand;
                            energyValue = priceToSellToUtility;
                            noAuctionProfit = (priceToSellToUtility * energy);
                            Send("environmentAgent", $"seller {energy}"); //sends a message to environment agent to let it know that this household agent is a seller
                        }
                        else
                        {
                            //if household agents demand equals its generation on initialisation
                            energy = 0;
                            profit = 0;
                            noAuctionProfit = 0;
                            Send("environmentAgent", "householdNA"); //sends a message to environment agent to let it know that this household agent is a neither a buyer of a seller
                            Send("environmentAgent", $"data N/A {profit} {noAuctionProfit}"); //sends a message to the environment agent containing its type, profit, and noAuctionProfit
                            Send("environmentAgent", "householdSatisfied"); //sends a message to the environment agent letting it know that it is satisfied (demand = generation)
                            Stop(); //stops this household agent
                        }
                        break;

                    case "auction": //this agent reacts to "auction" messages
                        Send("environmentAgent", $"reservePrice {priceToSellToUtility}"); //sends a message to the environment agent containing the minimum price that this seler would sell 1 kWh of energy for
                        break;
                    case "sold": //this agent reacts to "sold" messages
                        energy -= 1;
                        profit += Convert.ToInt32(parameters[0]);
                        if (energy == 0)
                        {
                            //if all excess energy has been sold
                            Send("environmentAgent", $"data Seller {profit} {noAuctionProfit}"); //sends a message to the environment agent containing its type, profit, and noAuctionProfit
                            Send("environmentAgent", "householdSatisfied"); //sends a message to the environment agent letting it know that it is satisfied (demand = generation)
                            Stop(); //stops this household agent
                        }
                        break;
                    case "notSold": //this agent reacts to "notSold" messages
                        profit += (priceToSellToUtility * energy); //sell remaining excess energy to utility company
                        energy = 0;
                        Send("environmentAgent", $"data Seller {profit} {noAuctionProfit}"); //sends a message to the environment agent containing its type, profit, and noAuctionProfit
                        Send("environmentAgent", "householdSatisfied"); //sends a message to the environment agent letting it know that it is satisfied (demand = generation)
                        Stop(); //stops this household agent
                        break;

                    case "price": //this agent reacts to "price" messages
                        if (buyer == true)
                        {
                            if (Convert.ToInt32(parameters[0]) <= energyValue)
                            {
                                //if this buyers energy value is less than or equal to the current asking price
                                Send("environmentAgent", "bid"); //sends a message to the environment agent to submit a bid to the current auction
                            }
                            else
                            {
                                //Stop();
                            }
                        }
                        break;
                    case "noSellers": //this agent reacts to "noSellers" messages
                        profit -= (priceToBuyFromUtility * energy); //buy remaining energy demand from the utility company
                        energy = 0;
                        Send("environmentAgent", $"data Buyer {profit} {noAuctionProfit}"); //sends a message to the environment agent containing its type, profit, and noAuctionProfit
                        Send("environmentAgent", "householdSatisfied"); //sends a message to the environment agent letting it know that it is satisfied (demand = generation)
                        Stop(); //stops this household agent
                        break;
                    case "winner": //this agent reacts to "winner" messages
                        if (parameters[0] == Name)
                        {
                            //if winner of the auction
                            Console.WriteLine($"[{Name}]: I have won.");
                            profit -= Convert.ToInt32(parameters[1]); //take away price paid from profit
                            energy -= 1;
                            if(energy == 0)
                            {
                                //if energy demand = generation
                                Send("environmentAgent", $"data Buyer {profit} {noAuctionProfit}"); //sends a message to the environment agent containing its type, profit, and noAuctionProfit
                                Send("environmentAgent", "householdSatisfied"); //sends a message to the environment agent letting it know that it is satisfied (demand = generation)
                                Stop(); //stops this household agent
                            }
                        }

                        //Stop();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
