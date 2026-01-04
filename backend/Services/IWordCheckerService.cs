using System;

namespace OddOneOut.Services
{
    public interface IWordCheckerService
    {
        string WordInvalidReason(string word);
    }
}