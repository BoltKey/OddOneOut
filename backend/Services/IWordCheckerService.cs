using System;

namespace OddOneOut.Services
{
    public interface IWordCheckerService
    {
        bool IsValidPlay(string word);
    }
}