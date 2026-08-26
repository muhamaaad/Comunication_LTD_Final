using CommunicationLTD.Services;

var hasher = new HmacPasswordHasher();

var (hash1, salt1) = hasher.Hash("MyPassword123!");
var (hash2, salt2) = hasher.Hash("MyPassword123!");

Console.WriteLine($"salt1==salt2: {salt1.SequenceEqual(salt2)}");
Console.WriteLine($"hash1==hash2: {hash1.SequenceEqual(hash2)}");
Console.WriteLine($"Verify correct: {hasher.Verify("MyPassword123!", hash1, salt1)}");
Console.WriteLine($"Verify wrong password: {hasher.Verify("WrongPassword!", hash1, salt1)}");
Console.WriteLine($"Verify wrong salt: {hasher.Verify("MyPassword123!", hash1, salt2)}");
