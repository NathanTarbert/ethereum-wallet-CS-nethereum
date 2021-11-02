using System;
using static System.Console;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.HdWallet;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NBitcoin;
using Rijndael256;
using System.Threading;

namespace Wallets
{
    class EthereumWallet
    {
        const string CURRENT_NETWORK = "https://ropsten.infura.io/v3/c1f509a577f44113adbcfe2bfc3505cc"; // TODO: Specify wich network you are going to use.
        const string WORKING_DIRECTORY = @"Wallets\"; // Path where you want to store the Wallets.

        static void Main(string[] args)
        {
            MainAsync();
        }

        static async void MainAsync()
        {
            // Available commands
            string[] availableOperations =
            {
                "create", "load", "recover", "exit"
            };

            string input = string.Empty;
            bool isWalletReady = false;
            Wallet wallet = new Wallet(Wordlist.English, WordCount.Twelve);

            // TODO: Initialize the Web3 instance and create the Storage Directory
            Web3 web3 = new Web3(CURRENT_NETWORK);
            Directory.CreateDirectory(WORKING_DIRECTORY);

            while (!input.ToLower().Equals("exit"))
            {
                if (!isWalletReady)
                {
                    do
                    {
                        input = ReceiveCommandCreateLoadOrRecover();

                    } while (!((IList)availableOperations).Contains(input));
                    switch (input)
                    {
                        /* Create a brand new wallet. 
                         * Users will receive a mnemonic phrase and public-private keypairs. */
                        case "create":
                            wallet = CreateWalletDialog();
                            isWalletReady = true;
                            break;

                        /* This command will decrypt the words and load wallet.
                         * Load wallet from JSON file containing an encrypted mnemonic phrase. */
                        case "load":
                            wallet = LoadWalletDialog();
                            isWalletReady = true;
                            break;

                        /* Recover wallet from mnemonic phrase which the user will provide.
                         * This is usefull if user already has an existing wallet, but has no json file for him 
                         * (for example, he uses this program for the first time).
                         * Command will create a new JSON file containing encrypted mnemonic phrase
                         * for this wallet.
                         * After encrypting the words and saving to disk, the program will load wallet.*/
                        case "recover":
                            wallet = RecoverWalletDialog();
                            isWalletReady = true;
                            break;

                        // Exit the program.
                        case "exit":
                            return;
                    }
                }
                else // Wallet already loaded.
                {
                    // Allowed functionalities
                    string[] walletAvailableOperations = {
                        "balance", "receive", "send", "exit"
                    };

                    string inputCommand = string.Empty;

                    while (!inputCommand.ToLower().Equals("exit"))
                    {
                        do
                        {
                            inputCommand = ReceiveCommandForEthersOperations();

                        } while (!((IList)walletAvailableOperations).Contains(inputCommand));
                        switch (inputCommand)
                        {
                            // Send transaction from own wallet to another address.
                            case "send":
                                SendTransactionDialog(wallet);
                                break;

                            // Shows the balances of addresses and total balance.
                            case "balance":
                                await GetWalletBallanceDialog(web3, wallet);
                                break;

                            // Shows available addresses under the control of your wallet which you can receive coins.
                            case "receive":
                                Receive(wallet);
                                break;
                            case "exit":
                                return;
                        }
                    }
                }
            }
        }

        // Preset codes: Dialogs ============================
        private static Wallet CreateWalletDialog()
        {
            try
            {
                string password;
                string passwordConfirmed;
                do
                {
                    Write("Enter password for encryption: ");
                    password = ReadLine();
                    Write("Confirm password: ");
                    passwordConfirmed = ReadLine();
                    if (password != passwordConfirmed)
                    {
                        WriteLine("Passwords did not match!");
                        WriteLine("Try again.");
                    }
                } while (password != passwordConfirmed);

                // Create new Wallet with the provided password.
                Wallet wallet = CreateWallet(password, WORKING_DIRECTORY);
                return wallet;
            }
            catch (Exception)
            {
                WriteLine($"ERROR! Wallet in path {WORKING_DIRECTORY} can`t be created!");
                throw;
            }
        }
        private static Wallet LoadWalletDialog()
        {
            Write("Enter: Name of the file containing wallet: ");
            string nameOfWallet = ReadLine();
            Write("Enter: Password: ");
            string pass = ReadLine();
            try
            {
                // Loading the Wallet from an JSON file. Using the Password to decrypt it.
                Wallet wallet = LoadWalletFromJsonFile(nameOfWallet, WORKING_DIRECTORY, pass);
                return (wallet);

            }
            catch (Exception e)
            {
                WriteLine($"ERROR! Wallet {nameOfWallet} in path {WORKING_DIRECTORY} can`t be loaded!");
                throw e;
            }
        }
        private static Wallet RecoverWalletDialog()
        {
            try
            {
                Write("Enter: Mnemonic words with single space separator: ");
                string mnemonicPhrase = ReadLine();
                Write("Enter: password for encryption: ");
                string passForEncryptionInJsonFile = ReadLine();

                // Recovering the Wallet from Mnemonic Phrase
                Wallet wallet = RecoverFromMnemonicPhraseAndSaveToJson(
                    mnemonicPhrase, passForEncryptionInJsonFile, WORKING_DIRECTORY);
                return wallet;
            }
            catch (Exception e)
            {
                WriteLine("ERROR! Wallet can`t be recovered! Check your mnemonic phrase.");
                throw e;
            }
        }
        private static async Task GetWalletBallanceDialog(Web3 web3, Wallet wallet)
        {
            WriteLine("Balance:");
            try
            {
                // Getting the Balance and Displaying the Information.
                Balance(web3, wallet);
            }
            catch (Exception)
            {
                WriteLine("Error occured! Check your wallet.");
            }
        }
        private static void SendTransactionDialog(Wallet wallet)
        {
            WriteLine("Enter: Address sending ethers.");
            string fromAddress = ReadLine();
            WriteLine("Enter: Address receiving ethers.");
            string toAddress = ReadLine();
            WriteLine("Enter: Amount of coins in ETH.");
            double amountOfCoins = 0d;
            try
            {
                amountOfCoins = double.Parse(ReadLine());
            }
            catch (Exception)
            {
                WriteLine("Unacceptable input for amount of coins.");
            }
            if (amountOfCoins > 0.0d)
            {
                WriteLine($"You will send {amountOfCoins} ETH from {fromAddress} to {toAddress}");
                WriteLine($"Are you sure? yes/no");
                string answer = ReadLine();
                if (answer.ToLower() == "yes")
                {
                    // Send the Transaction.
                    Send(wallet, fromAddress, toAddress, amountOfCoins);
                }
            }
            else
            {
                WriteLine("Amount of coins for transaction must be positive number!");
            }
        }
        private static string ReceiveCommandCreateLoadOrRecover()
        {
            WriteLine("Choose working wallet.");
            WriteLine("Choose [create] to Create new Wallet.");
            WriteLine("Choose [load] to load existing Wallet from file.");
            WriteLine("Choose [recover] to recover Wallet with Mnemonic Phrase.");
            Write("Enter operation [\"Create\", \"Load\", \"Recover\", \"Exit\"]: ");
            string input = ReadLine().ToLower().Trim();
            return input;
        }
        private static string ReceiveCommandForEthersOperations()
        {
            Write("Enter operation [\"Balance\", \"Receive\", \"Send\", \"Exit\"]: ");
            string inputCommand = ReadLine().ToLower().Trim();
            return inputCommand;
        }

        // End preset codes ============================

        // TODO: Implement these methods.

        public static Wallet CreateWallet(string password, string pathfile)
        {
            // TODO: Create a new wallet via a random 12-word mnemonic.

            try
            {
                // TODO: Save the Wallet in the Directory path declared earlier.
            }
            catch (Exception e)
            {
                WriteLine($"ERROR! The file {fileName} can`t be saved! {e}");
                throw e;
            }

            WriteLine("New Wallet was created successfully!");
            WriteLine("Write down the following mnemonic words and keep them in a safe place.");
            WriteLine("---");

            // TODO: Display the mnemonic phrase

            // TODO: Display the seed

            // TODO: Implement and use PrintAddressesAndKeys to print all the Addresses and Keys.

            return wallet;
        }

        private static void PrintAddressesAndKeys(Wallet wallet)
        {
            // TODO: Print all the Addresses and the coresponding Private Keys.
        }

        private static string SaveWalletToJsonFile(Wallet wallet, string password, string pathfile)
        {
            //TODO: Encrypt the wallet
            return fileName;
        }

        private static Wallet LoadWalletFromJsonFile(string nameOfWalletFile, string path, string password)
        {
            // TODO: Implement the logic that is needed to Load and Wallet from JSON.
            return Recover(words);
        }

        private static Wallet Recover(string words)
        {
            // TODO: Recover a Wallet from existing mnemonic phrase.
            return wallet;
        }

        public static Wallet RecoverFromMnemonicPhraseAndSaveToJson(string words, string password, string pathfile)
        {
            // TODO: Recover from mnemonic phrases and save to JSON.

            // TODO: Save the wallet to JSON.
            try
            {

            }
            catch (Exception e)
            {
                WriteLine($"Error! The file {fileName} cannot be saved: {e.Message}");
                throw e;
            }

            return wallet;
        }

        public static void Receive(Wallet wallet)
        {
            // TODO: Print all available addresses in Wallet.
        }

        private static void Send(Wallet wallet, string fromAddress, string toAddress, double amountOfCoins)
        {
            // TODO: Generate and Send a transaction.
            // Check if sending address is in the wallet by verifying if the private key exists.


            if (privateKeyFrom == string.Empty)
            {
                WriteLine("Keys of sending address is not found in current wallet.");
            }

            // Todo: Initialize web3 and normalize transaction value.

            // Todo: Broadcast transaction.
            try
            {

            }
            catch (Exception e)
            {
                WriteLine($"Error! The transaction can't be completed: {e.Message}");
                throw e;
            }
        }

        private static void Balance(Web3 web3, Wallet wallet)
        {
            // TODO: Print all addresses and their corresponding balance.
            decimal totalBalance = 0.0m;
            int NUMBER_OF_ITERATIONS = 20;

            // TODO: Print the balance of each address.
            // Track these balances and print the total balance of the wallet as well at the end.
            for (int i = 0; i < NUMBER_OF_ITERATIONS; i++)
            {

            }

            WriteLine($"Total balance: {totalBalance} ETH");
        }
    }
}
