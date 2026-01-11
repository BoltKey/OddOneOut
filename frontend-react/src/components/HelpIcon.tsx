import { useState, useEffect, useCallback } from "react";
import { FaQuestionCircle } from "react-icons/fa";
import "./HelpIcon.css";

interface HelpIconProps {
  content: React.ReactNode;
  title?: string;
}

export default function HelpIcon({ content, title }: HelpIconProps) {
  const [isOpen, setIsOpen] = useState(false);

  // Close popup handler that manages history
  const closePopup = useCallback(() => {
    if (isOpen && window.history.state?.helpPopup) {
      window.history.back();
    } else {
      setIsOpen(false);
    }
  }, [isOpen]);

  // Open popup and push history state
  const openPopup = useCallback(() => {
    window.history.pushState({ helpPopup: true }, '');
    setIsOpen(true);
  }, []);

  // Handle browser back button
  useEffect(() => {
    const handlePopState = () => {
      if (isOpen) {
        setIsOpen(false);
      }
    };

    window.addEventListener('popstate', handlePopState);
    return () => window.removeEventListener('popstate', handlePopState);
  }, [isOpen]);

  return (
    <div className="help-icon-container">
      <button
        type="button"
        className="help-icon-button"
        onClick={(e) => {
          e.stopPropagation();
          if (isOpen) {
            closePopup();
          } else {
            openPopup();
          }
        }}
        aria-label="Help"
      >
        <FaQuestionCircle />
      </button>
      {isOpen && (
        <>
          <div
            className="help-icon-overlay"
            onClick={closePopup}
          />
          <div className="help-icon-popup">
            {title && <div className="help-icon-title">{title}</div>}
            <div className="help-icon-content">{content}</div>
            <button
              className="help-icon-close"
              onClick={closePopup}
            >
              Close
            </button>
          </div>
        </>
      )}
    </div>
  );
}
