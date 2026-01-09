import { useState } from "react";
import { FaQuestionCircle } from "react-icons/fa";
import "./HelpIcon.css";

interface HelpIconProps {
  content: React.ReactNode;
  title?: string;
}

export default function HelpIcon({ content, title }: HelpIconProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="help-icon-container">
      <button
        type="button"
        className="help-icon-button"
        onClick={(e) => {
          e.stopPropagation();
          setIsOpen(!isOpen);
        }}
        aria-label="Help"
      >
        <FaQuestionCircle />
      </button>
      {isOpen && (
        <>
          <div
            className="help-icon-overlay"
            onClick={() => setIsOpen(false)}
          />
          <div className="help-icon-popup">
            {title && <div className="help-icon-title">{title}</div>}
            <div className="help-icon-content">{content}</div>
            <button
              className="help-icon-close"
              onClick={() => setIsOpen(false)}
            >
              Close
            </button>
          </div>
        </>
      )}
    </div>
  );
}
