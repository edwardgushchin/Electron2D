/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class TranslationTests
{
    [Fact]
    public void TranslationStoresLocaleMessagesAndContexts()
    {
        var translation = new Electron2D.Translation
        {
            Locale = "pt-BR"
        };

        translation.AddMessage("ui.play", "Jogar");
        translation.AddMessage("ui.play", "Iniciar", "menu");
        translation.AddMessage("ui.quit", "Sair");

        Assert.Equal("pt_BR", translation.Locale);
        Assert.Equal("Jogar", translation.GetMessage("ui.play"));
        Assert.Equal("Iniciar", translation.GetMessage("ui.play", "menu"));
        Assert.Equal(string.Empty, translation.GetMessage("ui.missing"));
        Assert.Equal(new[] { "ui.play", "ui.quit" }, translation.GetMessageList());

        translation.EraseMessage("ui.play", "menu");

        Assert.Equal(string.Empty, translation.GetMessage("ui.play", "menu"));
        Assert.Equal("Jogar", translation.GetMessage("ui.play"));
        Assert.Throws<ArgumentNullException>((Action)(() => translation.AddMessage(null!, "value")));
        Assert.Throws<ArgumentNullException>((Action)(() => translation.AddMessage("key", null!)));
        Assert.Throws<ArgumentNullException>((Action)(() => translation.GetMessage(null!)));
    }

    [Fact]
    public void TranslationServerUsesManualLocaleBaseLanguageFallbackAndKeyFallback()
    {
        var oldLocale = Electron2D.TranslationServer.GetLocale();
        Electron2D.TranslationServer.Clear();

        try
        {
            var english = new Electron2D.Translation
            {
                Locale = "en"
            };
            english.AddMessage("ui.score", "Score");

            var french = new Electron2D.Translation
            {
                Locale = "fr"
            };
            french.AddMessage("ui.score", "Score FR");
            french.AddMessage("ui.play", "Jouer", "menu");

            Electron2D.TranslationServer.AddTranslation(english);
            Electron2D.TranslationServer.AddTranslation(french);

            Electron2D.TranslationServer.SetLocale("fr-CA");

            Assert.Equal("fr_CA", Electron2D.TranslationServer.GetLocale());
            Assert.Equal("Score FR", Electron2D.TranslationServer.Translate("ui.score"));
            Assert.Equal("Jouer", Electron2D.TranslationServer.Translate("ui.play", "menu"));
            Assert.Equal("ui.missing", Electron2D.TranslationServer.Translate("ui.missing"));

            Electron2D.TranslationServer.SetLocale("de-DE");

            Assert.Equal("Score", Electron2D.TranslationServer.Translate("ui.score"));
            Assert.Equal(new[] { "en", "fr" }, Electron2D.TranslationServer.GetLoadedLocales());

            Electron2D.TranslationServer.RemoveTranslation(english);

            Assert.Equal(new[] { "fr" }, Electron2D.TranslationServer.GetLoadedLocales());
        }
        finally
        {
            Electron2D.TranslationServer.Clear();
            Electron2D.TranslationServer.SetLocale(oldLocale);
        }
    }
}
